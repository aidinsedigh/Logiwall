using System.Net;
using System.Reflection;
using System.Runtime.Loader;
using System.Web;
using Logiwall.Client.Core.Services;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Localization;
using Logiwall.Server.Services;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using Serilog;

namespace Logiwall.Server;

public static partial class Program
{
    /// <summary>
    /// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-8.0#middleware-order
    /// </summary>
    public static void ConfigureMiddlewares(this WebApplication app)
    {
        var configuration = app.Configuration;
        var env = app.Environment;

        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseSerilogRequestLogging(options =>
            options.MessageTemplate = "[Request][Complete][{RequestMethod}][{RequestPath}][{StatusCode}][{Elapsed}] Request completed.");
        app.UseForwardedHeaders();

        var persian = CultureInfoManager.CreateCultureInfo("fa-IR");
        app.UseRequestLocalization(new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture(persian),
            SupportedCultures = [persian],
            SupportedUICultures = [persian],
            RequestCultureProviders = [], // Persian only (ADR-0003): ignore Accept-Language, cookie and query.
            ApplyCurrentCultureToResponseHeaders = true
        });

        app.UseExceptionHandler("/", createScopeForErrors: true);

        if (env.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseHttpsRedirection();
            app.UseResponseCaching();
            app.UseResponseCompression();
        }

        Configure_401_403_404_Pages(app);

        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                if (env.IsDevelopment() is false)
                {
                    // https://bitplatform.dev/templates/cache-mechanism
                    ctx.Context.Response.GetTypedHeaders().CacheControl = new()
                    {
                        MaxAge = TimeSpan.FromDays(7),
                        Public = true
                    };
                }
            }
        });

        // 0.0.0.0 origins are essential for the proper functioning of BlazorHybrid's WebView, while localhost:4030 is a prerequisite for BlazorWebAssemblyStandalone testing.
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        app.UseCors(options =>
        {
            if (allowedOrigins.Length > 0)
                options.WithOrigins(allowedOrigins);
            else
                options.AllowAnyOrigin(); // Fallback keeps existing deployments working until Cors:AllowedOrigins is configured.
            options.AllowAnyHeader().AllowAnyMethod();
        });
        if (allowedOrigins.Length == 0 && env.IsDevelopment() is false)
        {
            app.Logger.LogWarning("Cors:AllowedOrigins is empty; CORS allows any origin. Configure the frontend origin(s).");
        }

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();

        app.UseAntiforgery();


        if (configuration.GetValue<bool>("Swagger:Enabled"))
        {
            app.UseSwagger();

            app.UseSwaggerUI(options =>
            {
                options.InjectJavascript($"/swagger/swagger-utils.js?v={Environment.TickCount64}");
            });
        }


        app.MapGet("/.well-known/apple-app-site-association", async () =>
        {
            // https://branch.io/resources/aasa-validator/ 
            var contentType = "application/json; charset=utf-8";
            var path = Path.Combine("wwwroot/.well-known", "apple-app-site-association");
            return Results.Stream(File.OpenRead(path), contentType, "apple-app-site-association");
        }).ExcludeFromDescription();


        // .NET 10 serves _framework/blazor.web.js as a static web asset, so UseStaticFiles alone no longer finds it.
        app.MapStaticAssets();

        app.MapControllers().RequireAuthorization();

        var appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>()!;

        var healthCheckSettings = appSettings.HealthCheckSettings;

        if (healthCheckSettings.EnableHealthChecks)
        {
            app.MapHealthChecks("/healthz", new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            app.MapHealthChecksUI(options =>
            {
                options.UseRelativeApiPath =
                    options.UseRelativeResourcesPath =
                        options.UseRelativeWebhookPath = false;
            });
        }


        // Handle the rest of requests with blazor
        var blazorApp = app.MapRazorComponents<Components.App>()
            .AddInteractiveServerRenderMode()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(AssemblyLoadContext.Default.Assemblies.Where(asm => asm.GetName().Name?.Contains("Logiwall") is true).Except([Assembly.GetExecutingAssembly()]).ToArray());

        if (AppRenderMode.PrerenderEnabled is false)
        {
            blazorApp.AllowAnonymous(); // Server may not check authorization for pages when there's no pre rendering, let the client handle it.
        }
    }

    /// <summary>
    /// Prior to the introduction of .NET 8, the Blazor router effectively managed NotFound and NotAuthorized components during pre-rendering.
    /// However, the current behavior has changed, and it now exclusively returns 401, 403, and 404 status codes with an empty body response!
    /// To address this, we've implemented the UseStatusCodePages middleware to handle responses featuring 401, 403, and 404 status codes that lack a body.
    /// This middleware facilitates redirection to the appropriate not-found and not-authorized pages. Consequently, the status code for these responses becomes 302 (Found).
    /// To mitigate the challenges posed by this situation, our only recourse is to repurpose the 401, 403, and 404 status codes for
    /// not-found and not-authorized responses, at the very least.
    /// </summary>
    private static void Configure_401_403_404_Pages(WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            int? statusCode = null;
            if (context.Request.Path.HasValue)
            {
                if (context.Request.Path.Value.Contains("not-found", StringComparison.InvariantCultureIgnoreCase))
                {
                    statusCode = (int)HttpStatusCode.NotFound;
                }
                if (context.Request.Path.Value.Contains("not-authorized", StringComparison.InvariantCultureIgnoreCase))
                {
                    statusCode = context.Request.Query["isForbidden"].FirstOrDefault() is "true" ? (int)HttpStatusCode.Forbidden : (int)HttpStatusCode.Unauthorized;
                }
            }

            if (statusCode is not null)
            {
                // Set the status just before the headers go out. On .NET 10, a 404 set before the Blazor page
                // renders makes UseStatusCodePages run the page a second time, which adds its headers twice.
                context.Response.OnStarting(() =>
                {
                    context.Response.StatusCode = statusCode.Value;
                    return Task.CompletedTask;
                });
            }

            await next.Invoke(context);
        });

        app.UseStatusCodePages(options: new()
        {
            HandleAsync = async (statusCodeContext) =>
            {
                var httpContext = statusCodeContext.HttpContext;

                if (httpContext.Response.StatusCode is 401 or 403 &&
                    httpContext.GetEndpoint()?.Metadata.OfType<ComponentTypeMetadata>().Any() is true /* The generation of a 401 or 403 status code is attributed to Blazor. */)
                {
                    bool is403 = httpContext.Response.StatusCode is 403;

                    var qs = HttpUtility.ParseQueryString(httpContext.Request.QueryString.Value ?? string.Empty);
                    qs.Remove("try_refreshing_token");
                    var redirectUrl = UriHelper.BuildRelative(httpContext.Request.PathBase, httpContext.Request.Path, new QueryString(qs.ToString()));
                    httpContext.Response.Redirect($"/not-authorized?redirect-url={redirectUrl}&isForbidden={(is403 ? "true" : "false")}");
                }
                else if (httpContext.Response.StatusCode is 404 &&
                    httpContext.GetEndpoint() is null /* Please be aware that certain endpoints, particularly those associated with web API actions, may intentionally return a 404 error. */)
                {
                    httpContext.Response.Redirect($"/not-found?url={httpContext.Request.GetEncodedPathAndQuery()}");
                }
                else
                {
                    await statusCodeContext.Next.Invoke(statusCodeContext.HttpContext);
                }
            }
        });
    }
}
