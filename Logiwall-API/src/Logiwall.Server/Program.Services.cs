using System.IO.Compression;
using Logiwall.Server.Services;
using Logiwall.Client.Web;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using System.Net.Mail;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.OData;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Logiwall.Domain.Entities.Identity;
using Logiwall.Infrastructure.Services;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace Logiwall.Server;

public static partial class Program
{
    private static void ConfigureServices(this WebApplicationBuilder builder)
    {
        // Services being registered here can get injected in server project only.

        var services = builder.Services;
        var configuration = builder.Configuration;
        var env = builder.Environment;

        services.AddExceptionHandler<ServerExceptionHandler>();

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.All;
            options.ForwardedHostHeaderName = "X-Host";
        });

        services.AddResponseCaching();

        services.AddHttpContextAccessor();

        services.AddResponseCompression(opts =>
        {
            opts.EnableForHttps = true;
            opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]).ToArray();
            opts.Providers.Add<BrotliCompressionProvider>();
            opts.Providers.Add<GzipCompressionProvider>();
        })
            .Configure<BrotliCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest)
            .Configure<GzipCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest);


        var appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>()!;

        services.AddCors();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("otp-send", ctx => RateLimitPartition.GetFixedWindowLimiter(
                ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = configuration.GetValue("RateLimits:OtpSendPerMinute", 5), Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            options.AddPolicy("auth-verify", ctx => RateLimitPartition.GetFixedWindowLimiter(
                ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = configuration.GetValue("RateLimits:AuthVerifyPerMinute", 20), Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });

        services.AddHttpClient();
        
    
        services
            .AddControllers()
            //.AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .AddOData(options => options.EnableQueryFeatures())
            .AddDataAnnotationsLocalization(options => options.DataAnnotationLocalizerProvider = StringLocalizerProvider.ProvideLocalizer)
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    throw new ResourceValidationException(context.ModelState.Select(ms => (ms.Key, ms.Value!.Errors.Select(e => new LocalizedString(e.ErrorMessage, e.ErrorMessage)).ToArray())).ToArray());
                };
            });

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("SqlServerConnectionString"), dbOptions =>
            {
                dbOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
        });

        services.Configure<AppSettings>(configuration.GetSection(nameof(AppSettings)));

        services.TryAddTransient(sp => sp.GetRequiredService<IOptionsSnapshot<AppSettings>>().Value);
        services.AddInfrastructure(configuration);
        services.AddEndpointsApiExplorer();

        AddSwaggerGen(builder);

        AddIdentity(builder);

        AddHealthChecks(builder);

        services.TryAddTransient<HtmlRenderer>();

        var fluentEmailServiceBuilder = services.AddFluentEmail(appSettings.EmailSettings.DefaultFromEmail, appSettings.EmailSettings.DefaultFromName);

        if (appSettings.EmailSettings.UseLocalFolderForEmails)
        {
            var sentEmailsFolderPath = Path.Combine(AppContext.BaseDirectory, "sent-emails");

            Directory.CreateDirectory(sentEmailsFolderPath);

            fluentEmailServiceBuilder.AddSmtpSender(() => new SmtpClient
            {
                DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                PickupDirectoryLocation = sentEmailsFolderPath
            });
        }
        else
        {
            if (appSettings.EmailSettings.HasCredential)
            {
                fluentEmailServiceBuilder.AddSmtpSender(() => new(appSettings.EmailSettings.Host, appSettings.EmailSettings.Port)
                {
                    Credentials = new NetworkCredential(appSettings.EmailSettings.UserName, appSettings.EmailSettings.Password),
                    EnableSsl = true
                });
            }
            else
            {
                fluentEmailServiceBuilder.AddSmtpSender(appSettings.EmailSettings.Host, appSettings.EmailSettings.Port);
            }
        }

        AddBlazor(builder);

    }

    private static void AddBlazor(WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        services.TryAddTransient<IAuthTokenProvider, ServerSideAuthTokenProvider>();

        services.TryAddTransient(sp =>
        {
            Uri.TryCreate(configuration.GetApiServerAddress(), UriKind.RelativeOrAbsolute, out var apiServerAddress);

            if (apiServerAddress!.IsAbsoluteUri is false)
            {
                apiServerAddress = new Uri(sp.GetRequiredService<IHttpContextAccessor>().HttpContext!.Request.GetBaseUrl(), apiServerAddress);
            }

            return new HttpClient(sp.GetRequiredKeyedService<HttpMessageHandler>("DefaultMessageHandler"))
            {
                BaseAddress = apiServerAddress
            };
        });

        services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();

        services.AddMvc();

       services.AddClientWebProjectServices();
    }

    private static void AddIdentity(WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;
        var env = builder.Environment;
        var appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>()!;
        var settings = appSettings.IdentitySettings;

        var certificatePath = Path.Combine(AppContext.BaseDirectory, "IdentityCertificate.pfx");
        var certificate = new X509Certificate2(certificatePath, appSettings.IdentitySettings.IdentityCertificatePassword, OperatingSystem.IsWindows() ? X509KeyStorageFlags.EphemeralKeySet : X509KeyStorageFlags.DefaultKeySet);

        bool isTestCertificate = certificate.Thumbprint is "55140A8C935AB5202949071E5781E6946CD60606"; // The default test certificate is still in use
        if (isTestCertificate && env.IsDevelopment() is false)
        {
            throw new InvalidOperationException(@"The default test certificate is still in use. Please replace it with a new one by running the 'dotnet dev-certs https --export-path IdentityCertificate.pfx --password P@ssw0rdP@ssw0rd' command (or your preferred method for generating PFX files) in the server project's folder.");
        }

        services.AddDataProtection()
            .PersistKeysToDbContext<AppDbContext>()
            .ProtectKeysWithCertificate(certificate);

        services.AddIdentity<User, Role>(options =>
        {
            options.Password.RequireDigit = settings.PasswordRequireDigit;
            options.Password.RequireLowercase = settings.PasswordRequireLowercase;
            options.Password.RequireUppercase = settings.PasswordRequireUppercase;
            options.Password.RequireNonAlphanumeric = settings.PasswordRequireNonAlphanumeric;
            options.Password.RequiredLength = settings.PasswordRequiredLength;
        })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders()
            .AddErrorDescriber<AppIdentityErrorDescriber>()
            .AddApiEndpoints();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = IdentityConstants.BearerScheme;
            options.DefaultChallengeScheme = IdentityConstants.BearerScheme;
            options.DefaultScheme = IdentityConstants.BearerScheme;
        })
        .AddBearerToken(IdentityConstants.BearerScheme, options =>
        {
            options.BearerTokenExpiration = settings.BearerTokenExpiration;
            options.RefreshTokenExpiration = settings.RefreshTokenExpiration;

            var validationParameters = new TokenValidationParameters
            {
                ClockSkew = TimeSpan.Zero,
                RequireSignedTokens = true,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new X509SecurityKey(certificate),

                RequireExpirationTime = true,
                ValidateLifetime = true,

                ValidateAudience = true,
                ValidAudience = settings.Audience,

                ValidateIssuer = true,
                ValidIssuer = settings.Issuer,

                AuthenticationType = IdentityConstants.BearerScheme
            };

            options.BearerTokenProtector = new AppSecureJwtDataFormat(appSettings, validationParameters);

            options.Events = new()
            {
                OnMessageReceived = async context =>
                {
                    // The server accepts the access_token from either the authorization header, the cookie, or the request URL query string
                    context.Token ??= context.Request.Cookies["access_token"] ?? context.Request.Query["access_token"];
                }
            };
        });

        services.AddAuthorization();
    }

    private static void AddSwaggerGen(WebApplicationBuilder builder)
    {
        var services = builder.Services;

        services.AddSwaggerGen(options =>
        {
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Logiwall.Server.xml"));
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Logiwall.Shared.xml"));

            //options.SchemaFilter<EnumNameValueSchemaFilter>();
            options.OperationFilter<ODataOperationFilter>();
            options.AddSecurityDefinition("bearerAuth", new()
            {
                Name = "Authorization",
                Description = "Enter the Bearer Authorization string as following: `Bearer Generated-Bearer-Token`",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new()
            {
                {
                    new()
                    {
                        Name = "Bearer",
                        In = ParameterLocation.Header,
                        Reference = new OpenApiReference
                        {
                            Id = "Bearer",
                            Type = ReferenceType.SecurityScheme
                        }
                    },
                    []
                }
            });
        });
    }

    private static void AddHealthChecks(WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var services = builder.Services;
        var env = builder.Environment;

        var appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>()!;

        var healthCheckSettings = appSettings.HealthCheckSettings;

        if (healthCheckSettings.EnableHealthChecks is false)
            return;

        services.AddHealthChecksUI(setupSettings: setup =>
        {
            setup.AddHealthCheckEndpoint("BPHealthChecks", env.IsDevelopment() ? "http://localhost:5030/healthz" : "/healthz");
        }).AddInMemoryStorage();

        var healthChecksBuilder = services.AddHealthChecks()
            .AddProcessAllocatedMemoryHealthCheck(maximumMegabytesAllocated: 6 * 1024)
            .AddDiskStorageHealthCheck(opt =>
                opt.AddDrive(Path.GetPathRoot(Directory.GetCurrentDirectory())!, minimumFreeMegabytes: 5 * 1024))
            .AddDbContextCheck<AppDbContext>();

        var emailSettings = appSettings.EmailSettings;

        if (emailSettings.UseLocalFolderForEmails is false)
        {
            healthChecksBuilder
                .AddSmtpHealthCheck(options =>
                {
                    options.Host = emailSettings.Host;
                    options.Port = emailSettings.Port;

                    if (emailSettings.HasCredential)
                    {
                        options.LoginWith(emailSettings.UserName, emailSettings.Password);
                    }
                });
        }
    }

}
