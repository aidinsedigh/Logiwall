using Logiwall.Application.Abstractions.Global;
using Logiwall.Domain.Entities.OrderItems;
using Logiwall.Infrastructure.Data;
using Logiwall.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Logiwall.Infrastructure.Services.OrderItems;

public class ShippingWorker(IServiceScopeFactory scopes, IOptions<ETipaxOptions> options,
    ILogger<ShippingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                if (options.Value.ReferenceSyncEnabled)
                    await RunAsync("shipping-reference-sync", Math.Clamp(options.Value.ReferenceSyncMinutes, 10, 10080),
                        async (scope, ct) => await scope.ServiceProvider.GetRequiredService<IShippingReferenceService>().SynchronizeAsync(ct), stoppingToken);
                if (options.Value.ReconcileEventsEnabled)
                    await RunAsync("shipping-event-reconciliation", 1,
                        async (scope, ct) => await scope.ServiceProvider.GetRequiredService<ShipmentService>().ReconcileEventsAsync(ct), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception e) { logger.LogWarning(e, "[ShippingWorker][Execute] Scheduled shipping work failed."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunAsync(string name, int intervalMinutes, Func<IServiceScope, CancellationToken, Task> action, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var owner = Guid.NewGuid();
        // Seeded rows and an atomic conditional update provide a cross-instance lease.
        var claimed = await db.ShippingJobLeases.Where(l => l.Name == name && l.ExpiresAt <= now)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.Owner, owner).SetProperty(l => l.ExpiresAt, now.AddMinutes(5)), ct);
        if (claimed == 0) return;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromMinutes(4));
        var success = false;
        try { await action(scope, deadline.Token); success = true; }
        finally
        {
            db.ChangeTracker.Clear();
            using var releaseDeadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var due = DateTimeOffset.UtcNow.AddMinutes(success ? intervalMinutes : 5);
            await db.ShippingJobLeases.Where(l => l.Name == name && l.Owner == owner)
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.ExpiresAt, due), releaseDeadline.Token);
        }
    }
}
