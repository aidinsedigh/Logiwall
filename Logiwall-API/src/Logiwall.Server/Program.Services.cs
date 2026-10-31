using System.IO.Compression;
using Logiwall.Server.Services;

using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using System.Net.Mail;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Logiwall.Domain.Entities.Identity;
using Logiwall.Infrastructure.Services;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;

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
            // Only client IP and scheme are taken from the proxy; Host is never overridden by a header.
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            if (configuration.GetValue<bool>("ForwardedHeaders:TrustAllProxies"))
            {
                // Safe only when the app is reachable exclusively through the platform proxy (e.g. Azure App Service).
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            }
            foreach (var proxy in configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
            {
                options.KnownProxies.Add(System.Net.IPAddress.Parse(proxy));
            }
        });

        services.AddResponseCaching();

        services.AddHttpContextAccessor();
        services.AddAntiforgery();

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
            options.OnRejected = async (context, cancellationToken) =>
            {
                var httpContext = context.HttpContext;
                var requestServices = httpContext.RequestServices;
                var policy = httpContext.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;
                requestServices.GetRequiredService<ILoggerFactory>().CreateLogger("RateLimit")
                    .LogWarning("[RateLimit][Reject][{Path}][{Policy}] Request was rate limited.", httpContext.Request.Path.Value, policy);

                var localizer = requestServices.GetRequiredService<IStringLocalizer<AppStrings>>();
                var jsonSerializerOptions = requestServices.GetRequiredService<JsonSerializerOptions>();
                await httpContext.Response.WriteAsJsonAsync(new RestErrorInfo
                {
                    Key = nameof(TooManyRequestsExceptions),
                    Message = localizer[nameof(TooManyRequestsExceptions)],
                    ExceptionType = typeof(TooManyRequestsExceptions).FullName
                }, jsonSerializerOptions.GetTypeInfo<RestErrorInfo>(), cancellationToken: cancellationToken);
            };
            options.AddPolicy("otp-send", ctx => RateLimitPartition.GetFixedWindowLimiter(
                ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = configuration.GetValue("RateLimits:OtpSendPerMinute", 5), Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            options.AddPolicy("auth-verify", ctx => RateLimitPartition.GetFixedWindowLimiter(
                ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = configuration.GetValue("RateLimits:AuthVerifyPerMinute", 20), Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });

        services.AddHttpClient();
        
    
        services
            .AddControllers(options =>
            {
                // Resource keys without placeholders: the placeholders are English property names (ADR-0003).
                var messages = options.ModelBindingMessageProvider;
                messages.SetMissingBindRequiredValueAccessor(_ => nameof(AppStrings.ModelBindingValueIsRequired));
                messages.SetMissingKeyOrValueAccessor(() => nameof(AppStrings.ModelBindingValueIsRequired));
                messages.SetMissingRequestBodyRequiredValueAccessor(() => nameof(AppStrings.ModelBindingValueIsRequired));
                messages.SetValueMustNotBeNullAccessor(_ => nameof(AppStrings.ModelBindingValueIsRequired));
                messages.SetAttemptedValueIsInvalidAccessor((_, _) => nameof(AppStrings.ModelBindingValueIsInvalid));
                messages.SetNonPropertyAttemptedValueIsInvalidAccessor(_ => nameof(AppStrings.ModelBindingValueIsInvalid));
                messages.SetUnknownValueIsInvalidAccessor(_ => nameof(AppStrings.ModelBindingValueIsInvalid));
                messages.SetNonPropertyUnknownValueIsInvalidAccessor(() => nameof(AppStrings.ModelBindingValueIsInvalid));
                messages.SetValueIsInvalidAccessor(_ => nameof(AppStrings.ModelBindingValueIsInvalid));
                messages.SetValueMustBeANumberAccessor(_ => nameof(AppStrings.ModelBindingValueIsInvalid));
                messages.SetNonPropertyValueMustBeANumberAccessor(() => nameof(AppStrings.ModelBindingValueIsInvalid));
            })
            //.AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .AddJsonOptions(options => options.AllowInputFormatterExceptionMessages = false)
            .AddDataAnnotationsLocalization(options => options.DataAnnotationLocalizerProvider = StringLocalizerProvider.ProvideLocalizer)
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var localizer = context.HttpContext.RequestServices.GetRequiredService<IStringLocalizer<AppStrings>>();
                    throw new ResourceValidationException(context.ModelState.Select(ms => (ms.Key, ms.Value!.Errors.Select(e => ToPersianError(e, localizer)).ToArray())).ToArray());
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

        services.AddSharedProjectServices();
        services.TryAddTransient(_ => AppJsonContext.Default.Options);

    }

    /// <summary>
    /// A model error as a Persian message (ADR-0003). A JSON-format error gets <c>InvalidRequestFormat</c>.
    /// Text that is not Persian (an attribute without a resource key, framework text) gets <c>ModelBindingValueIsInvalid</c>.
    /// </summary>
    private static LocalizedString ToPersianError(ModelError error, IStringLocalizer<AppStrings> localizer)
    {
        if (error.Exception is not null || string.IsNullOrEmpty(error.ErrorMessage))
            return localizer[nameof(AppStrings.InvalidRequestFormat)];

        var message = localizer[error.ErrorMessage];
        return message.Value.Any(char.IsAsciiLetter) ? localizer[nameof(AppStrings.ModelBindingValueIsInvalid)] : message;
    }

    private static void AddBlazor(WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        services.TryAddTransient<IAuthTokenProvider, ServerSideAuthTokenProvider>();

        // AddHttpClient() already registers an HttpClient with no BaseAddress, so a TryAdd here never runs.
        services.AddTransient(sp =>
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
        var certificate = X509CertificateLoader.LoadPkcs12FromFile(certificatePath, appSettings.IdentitySettings.IdentityCertificatePassword, OperatingSystem.IsWindows() ? X509KeyStorageFlags.EphemeralKeySet : X509KeyStorageFlags.DefaultKeySet);

        bool isTestCertificate = certificate.Thumbprint is "8C8176BAD0AEB08C998EE7D41512940AABA837AA"; // The committed development certificate is still in use
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

        services.AddHealthChecks()
            .AddProcessAllocatedMemoryHealthCheck(maximumMegabytesAllocated: 6 * 1024)
            .AddDiskStorageHealthCheck(opt =>
                opt.AddDrive(Path.GetPathRoot(Directory.GetCurrentDirectory())!, minimumFreeMegabytes: 5 * 1024))
            .AddDbContextCheck<AppDbContext>();
    }

}
