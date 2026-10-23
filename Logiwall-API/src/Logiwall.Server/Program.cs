
using Serilog.Events;
using Serilog;

namespace Logiwall.Server;

public static partial class Program
{
    public static async Task Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // Set minimum log level for Microsoft components
            .Enrich.FromLogContext()
            .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day) // Log to file with daily rotation
            .CreateLogger();
        try
        {
            Log.Information("Starting web host");

            var builder = WebApplication.CreateBuilder(args);
            // Add Serilog to logging
            builder.Host.UseSerilog();
            builder.Configuration.AddClientConfigurations();

            // The following line (using the * in the URL), allows the emulators and mobile devices to access the app using the host IP address.
            if (BuildConfiguration.IsDebug() && OperatingSystem.IsWindows())
            {
                builder.WebHost.UseUrls("http://localhost:5030", "http://*:5030");
            }

            builder.ConfigureServices();

            var app = builder.Build();

            app.ConfiureMiddlewares();

            await app.RunAsync();
        }
        catch (Exception exc)
        {
            Log.Fatal(exc, "Host terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
