
using Serilog;

namespace Logiwall.Server;

public static partial class Program
{
    public static async Task Main(string[] args)
    {
        // Bootstrap logger for start-up and fatal errors; UseSerilog replaces it with the configured one.
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();
        try
        {
            Log.Information("[Program][Main] Web host is starting.");

            var builder = WebApplication.CreateBuilder(args);
            // Levels and sinks come from the Serilog section of appsettings.json (ADR-0004).
            builder.Host.UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services));
            builder.Configuration.AddClientConfigurations();

            // The following line (using the * in the URL), allows the emulators and mobile devices to access the app using the host IP address.
            if (BuildConfiguration.IsDebug() && OperatingSystem.IsWindows())
            {
                builder.WebHost.UseUrls("http://localhost:5030", "http://*:5030");
            }

            builder.ConfigureServices();

            var app = builder.Build();

            app.ConfigureMiddlewares();

            await app.RunAsync();
        }
        catch (Exception exc) when (exc is not HostAbortedException)
        {
            Log.Fatal(exc, "[Program][Main] Web host stopped unexpectedly.");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
