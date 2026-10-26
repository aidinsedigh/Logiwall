

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using Serilog;
using Logiwall.Server.Middleware;

namespace Logiwall.Server;

public static partial class Program
{
    /// <summary>
    /// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-8.0#middleware-order
    /// </summary>
    public static void ConfiureMiddlewares(this WebApplication app)
    {
        var configuration = app.Configuration;
        var env = app.Environment;

        app.UseSerilogRequestLogging();
        //app.UseRequestResponseLoggingMiddleware();
        app.UseForwardedHeaders();

#if MultilingualEnabled
        {
            var supportedCultures = CultureInfoManager.SupportedCultures.Select(sc => CultureInfoManager.CreateCultureInfo(sc.code)).ToArray();
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures,
                ApplyCurrentCultureToResponseHeaders = true
            }.SetDefaultCulture(CultureInfoManager.DefaultCulture.code));
        }

#endif

        app.UseExceptionHandler("/", createScopeForErrors: true);

        if (!env.IsDevelopment())
        {
            app.UseHttpsRedirection();
            app.UseResponseCaching();
            app.UseResponseCompression();
        }



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
        app.UseCors(options => options.AllowAnyOrigin()
            .AllowAnyHeader().AllowAnyMethod());

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseAntiforgery();


        app.UseSwagger();

        app.UseSwaggerUI(options =>
        {
            options.InjectJavascript($"/swagger/swagger-utils.js?v={Environment.TickCount64}");
        });

        app.MapGet("/api/minimal-api-sample/{routeParameter}", (string routeParameter, [FromQuery] string queryStringParameter) => new
        {
            RouteParameter = routeParameter,
            QueryStringParameter = queryStringParameter
        }).WithTags("Test");


        app.MapGet("/.well-known/apple-app-site-association", async () =>
        {
            // https://branch.io/resources/aasa-validator/ 
            var contentType = "application/json; charset=utf-8";
            var path = Path.Combine("wwwroot/.well-known", "apple-app-site-association");
            return Results.Stream(File.OpenRead(path), contentType, "apple-app-site-association");
        }).ExcludeFromDescription();


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


        app.MapGet("/", () => Results.Redirect("/swagger"));
    }

}
