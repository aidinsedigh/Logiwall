using System.Globalization;
using System.Text.Json;
using Logiwall.Application.Abstractions.Global;
using Logiwall.Application.Abstractions.Providers;
using Logiwall.Domain.Entities.OrderItems;
using Logiwall.Infrastructure.Data;
using Logiwall.Shared.Dtos.OrderItems;
using Microsoft.EntityFrameworkCore;

namespace Logiwall.Infrastructure.Services.Global;

public class ShippingReferenceService(AppDbContext db, IEnumerable<IShippingCarrier> carriers) : IShippingReferenceService
{
    public async Task<IReadOnlyList<ShippingReferenceResponseDto>> GetAsync(string providerCode, string category, CancellationToken cancellationToken)
    {
        if (providerCode.Equals("tipax", StringComparison.OrdinalIgnoreCase)) providerCode = "etipax";
        if (category is not ("packaging" or "content" or "status" or "service")) throw new BadRequestException("ShippingReferenceInvalid");
        var rows = await db.ProviderReferenceItems.AsNoTracking().Where(r => r.ServiceProvider.Code == providerCode
            && r.ServiceProvider.IsActive && r.IsActive && r.Category == category).OrderBy(r => r.Title).Take(2000)
            .Select(r => new { r.Id, r.Category, r.Title, r.MetadataJson, r.ExternalCode, r.MappedOrderStatus }).ToListAsync(cancellationToken);
        return rows.Select(r =>
        {
            using var json = JsonDocument.Parse(r.MetadataJson);
            int? packType = json.RootElement.TryGetProperty("packType", out var value)
                && int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? number : null;
            return new ShippingReferenceResponseDto(r.Id, r.Category, r.Title, packType)
            { ExternalCode = r.ExternalCode, MappedOrderStatus = (short?)r.MappedOrderStatus };
        }).ToList();
    }

    public async Task SynchronizeAsync(CancellationToken cancellationToken)
    {
        foreach (var carrier in carriers)
        {
            var provider = await db.ServiceProviders.SingleAsync(p => p.Code == carrier.Code && p.IsActive, cancellationToken);
            // Fetch and validate every catalogue before changing any previous data.
            var items = await carrier.GetReferencesAsync(cancellationToken);
            if (items.GroupBy(x => (x.Category, x.Code)).Any(g => g.Count() > 1))
                throw new InvalidOperationException("Duplicate carrier reference codes.");
            await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT Id FROM ServiceProviders WITH (UPDLOCK,HOLDLOCK) WHERE Id={provider.Id}", cancellationToken);
            var old = await db.ProviderReferenceItems.Where(r => r.ServiceProviderId == provider.Id).ToListAsync(cancellationToken);
            foreach (var category in items.Select(i => i.Category).Distinct())
                foreach (var row in old.Where(r => r.Category == category)) row.IsActive = false;
            foreach (var item in items)
            {
                var row = old.SingleOrDefault(r => r.Category == item.Category && r.ExternalCode == item.Code);
                if (row is null)
                {
                    row = new ProviderReferenceItem { ServiceProviderId = provider.Id, Category = item.Category, ExternalCode = item.Code };
                    db.ProviderReferenceItems.Add(row);
                }
                row.Title = item.Title; row.MetadataJson = item.MetadataJson; row.IsActive = true; row.SyncedAt = DateTimeOffset.UtcNow;
            }
            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
    }
}
