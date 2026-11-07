using System.Globalization;
using System.Text.Json;
using Logiwall.Application.Abstractions.OrderItems;
using Logiwall.Application.Abstractions.Providers;
using Logiwall.Domain.Entities.OrderItems;
using Logiwall.Infrastructure.Data;
using Logiwall.Infrastructure.Providers.PostalServices;
using Logiwall.Shared.Dtos.Global;
using Logiwall.Shared.Dtos.OrderItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceProvider = Logiwall.Domain.Entities.OrderItems.ServiceProvider;

namespace Logiwall.Infrastructure.Services.OrderItems;

public class CarrierQuoteService(AppDbContext db, IEnumerable<IShippingCarrier> carriers,
    IStringLocalizer<AppStrings> localizer, ILogger<CarrierQuoteService> logger) : ICarrierQuoteService
{
    public async Task<ServiceResponse<IList<OrderProviderOptionsDto>>> GetAsync(int orderId, int userId, CancellationToken cancellationToken,
        Constants.PaymentMethod paymentMethod = Constants.PaymentMethod.CashOnDelivery, int? serviceReferenceId = null)
    {
        var ct = cancellationToken;
        if (!Constants.SelectablePaymentMethods.Contains(paymentMethod)) throw new BadRequestException("ShippingPaymentMethodMismatch");
        var order = await db.Orders.AsNoTracking().Include(x => x.OrderParcels).SingleOrDefaultAsync(x => x.Id == orderId && x.Customer.Person.UserId == userId, ct);
        if (order is null) throw new ResourceNotFoundException(nameof(AppStrings.InvalidOrder));
        if (await OrderDraftWorkflow.IsLockedAsync(db, orderId, ct))
            throw new ConflictException("ShippingOrderLocked");
        // No service given: quote every active service and offer only the ones the carrier accepts for this parcel.
        var quoteAll = serviceReferenceId is null;
        var services = await db.ProviderReferenceItems.AsNoTracking().Include(r => r.ServiceProvider)
            .Where(r => r.Category == "service" && r.IsActive && r.ServiceProvider.IsActive && r.ServiceProvider.Code != null
                && (quoteAll || r.Id == serviceReferenceId)).ToListAsync(ct);
        if (services.Count == 0)
            throw new BadRequestException(quoteAll ? "ShippingServiceCatalogMissing" : "ShippingServiceSelectionRequired");
        var newQuotes = new List<OrderInqueryOption>();
        var serviceTitles = new Dictionary<(short, int), string>();
        foreach (var group in services.GroupBy(s => s.ServiceProviderId))
        {
            var provider = group.First().ServiceProvider;
            var carrier = carriers.SingleOrDefault(x => x.Code == provider.Code);
            if (carrier is null) continue;
            var request = await BuildAsync(order, provider, ct);
            var requests = group.Select(s => request with { PaymentType = (int)paymentMethod,
                ServiceId = int.Parse(s.ExternalCode, CultureInfo.InvariantCulture) }).ToList();
            foreach (var s in group) serviceTitles[(provider.Id, int.Parse(s.ExternalCode, CultureInfo.InvariantCulture))] = s.Title;
            // The first call caches the carrier token, so the parallel calls after it do not each log in.
            var first = await TryQuoteAsync(carrier, requests[0], rethrowRejection: !quoteAll, orderId, ct);
            var rest = await Task.WhenAll(requests.Skip(1).Select(r => TryQuoteAsync(carrier, r, rethrowRejection: !quoteAll, orderId, ct)));
            foreach (var quote in rest.Prepend(first).SelectMany(q => q))
                newQuotes.Add(new OrderInqueryOption { OrderId = orderId, ServiceProviderId = provider.Id,
                    ServiceAmount = RoundUpPrice(quote.AmountRials), FinalAmount = RoundUpPrice(quote.AmountRials),
                    CarrierRequestJson = quote.RequestJson, ExternalServiceId = quote.ServiceId,
                    PaymentMethod = paymentMethod,
                    ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15), DelivereDate = string.Empty });
        }
        if (newQuotes.Count == 0)
            return new() { Succuss = false, Message = localizer[nameof(AppStrings.ProviderServiceUnavailable)] };
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT Id FROM Orders WITH (UPDLOCK,HOLDLOCK) WHERE Id={orderId}", ct);
        if (await OrderDraftWorkflow.IsLockedAsync(db, orderId, ct)) throw new ConflictException("ShippingOrderLocked");
        var current = await db.Orders.SingleAsync(x => x.Id == orderId, ct);
        if (current.ModificationDate != order.ModificationDate)
            throw new ConflictException("ShippingOrderChanged");
        await db.OrderInqueryOptions.Where(x => x.OrderId == orderId).ExecuteDeleteAsync(ct);
        OrderDraftWorkflow.ClearSelection(current);
        db.OrderInqueryOptions.AddRange(newQuotes);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        var titles = services.Select(s => s.ServiceProvider).DistinctBy(p => p.Id).ToDictionary(x => x.Id, x => x.Title);
        return new() { Succuss = true, Data = newQuotes.GroupBy(x => x.ServiceProviderId)
            .Select(g => new OrderProviderOptionsDto(g.Key, titles[g.Key], g.OrderBy(q => q.FinalAmount).Select(q => new OrderInqueryOptionsDto
            { Id = q.Id, BaseAmount = q.ServiceAmount, TotalAmount = q.FinalAmount, DelivereDate = q.DelivereDate,
                PaymentType = q.PaymentMethod, ServiceTitle = serviceTitles[(q.ServiceProviderId, q.ExternalServiceId!.Value)]
            }).ToList())).ToList() };
    }

    /// <summary>Rounds a carrier price up to the next 1,000 rials, so the customer never pays less than the carrier asks.</summary>
    private static decimal RoundUpPrice(decimal rials) => Math.Ceiling(rials / 1000m) * 1000m;

    /// <summary>Returns the carrier's quotes, or none when the carrier fails or (unless <paramref name="rethrowRejection"/>) rejects the request.</summary>
    private async Task<IReadOnlyList<CarrierQuote>> TryQuoteAsync(IShippingCarrier carrier, CarrierShipmentRequest request,
        bool rethrowRejection, int orderId, CancellationToken ct)
    {
        try
        {
            var quotes = await carrier.QuoteAsync(request, ct);
            if (quotes.Any(q => q.ServiceId != request.ServiceId)) throw new JsonException("Carrier returned a different service.");
            return quotes;
        }
        catch (Exception e) when (e is HttpRequestException or JsonException or InvalidOperationException
            || e is CarrierRequestException && !rethrowRejection
            || e is OperationCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogWarning(e, "[CarrierQuote][Get][{OrderId}][{ProviderCode}][{ServiceId}] Carrier quotation failed.", orderId, carrier.Code, request.ServiceId);
            return [];
        }
    }

    private async Task<CarrierShipmentRequest> BuildAsync(Order order, ServiceProvider provider, CancellationToken ct)
    {
        if (provider.Code != "etipax") throw new InvalidOperationException("Unsupported city mapping provider.");
        var ids = new[] { (int)order.OriginCity, (int)order.DestinationCity };
        var maps = await db.StaticDataMappers.AsNoTracking().Where(x => ids.Contains(x.StaticDataId) && x.ProviderType == ProviderType.ETipax).ToListAsync(ct);
        string City(int id) => maps.SingleOrDefault(x => x.StaticDataId == id)?.Code ?? throw new BadRequestException("ShippingCityNotMapped");
        var selections = order.OrderParcels.SelectMany(p => new[] { p.PackagingReferenceId, p.ContentReferenceId }).OfType<int>().Distinct().ToArray();
        var needsNoPackaging = order.OrderParcels.Any(p => p.PackageTypeId == 0);
        var refs = await db.ProviderReferenceItems.AsNoTracking().Where(x => x.IsActive && x.ServiceProviderId == provider.Id
            && (selections.Contains(x.Id) || needsNoPackaging && x.Category == "packaging"
                && (x.ExternalCode == "504" || x.ExternalCode == "521"))).ToListAsync(ct);
        ResourceValidationException FieldError(string field, string key, int index) =>
            new(key, new ErrorResourcePayload { Details =
                [new() { Name = $"orderParcels[{index}].{field}", Errors = [new() { Key = key, Message = localizer[key] }] }] });
        ProviderReferenceItem Ref(int? id, string category, int index)
        {
            var reference = refs.SingleOrDefault(x => x.Id == id && x.Category == category);
            if (reference is not null) return reference;
            var field = category == "packaging" ? "packagingReferenceId" : "contentReferenceId";
            var key = category == "packaging" ? "ShippingPackagingRequired" : "ShippingContentRequired";
            throw new ResourceValidationException(key, new ErrorResourcePayload { Details =
                [new() { Name = $"orderParcels[{index}].{field}", Errors = [new() { Key = key, Message = localizer[key] }] }] });
        }
        var originCity = City(order.OriginCity);
        var destinationCity = City(order.DestinationCity);
        var parcels = new List<CarrierParcel>();
        foreach (var p in order.OrderParcels.OrderBy(p => p.Id))
        {
            var content = Ref(p.ContentReferenceId, "content", parcels.Count);
            var contentType = ETipaxPackaging.PackType(content.MetadataJson);
            ProviderReferenceItem packing;
            if (p.PackageTypeId == 0 && p.PackagingReferenceId is null)
            {
                packing = refs.SingleOrDefault(r => r.Category == "packaging" && ETipaxPackaging.IsNoPackaging(r.ExternalCode)
                    && contentType is not null && ETipaxPackaging.PackType(r.MetadataJson) == contentType)
                    ?? throw FieldError("contentReferenceId", "ShippingNoPackagingUnavailable", parcels.Count);
            }
            else
            {
                packing = Ref(p.PackagingReferenceId, "packaging", parcels.Count);
                if (ETipaxPackaging.IsNoPackaging(packing.ExternalCode) != (p.PackageTypeId == 0))
                    throw FieldError("packagingReferenceId", "ShippingPackagingIncompatible", parcels.Count);
            }
            var type = ETipaxPackaging.PackType(packing.MetadataJson)
                ?? throw new BadRequestException("ShippingReferenceInvalid");
            if ((contentType is not null && contentType != type) || (p.PackageTypeId == 0 && contentType is null))
                throw FieldError("contentReferenceId", "ShippingPackagingIncompatible", parcels.Count);
            if (p.Weight <= 0 || p.Length <= 0 || p.Width <= 0 || p.Height <= 0 || p.ParcelValue <= 0 || p.COD < 0)
                throw new BadRequestException("ShippingParcelInvalid");
            CarrierAddress Address(bool origin) => new(origin ? originCity : destinationCity,
                (origin ? order.OriginAddress : order.DestinationAddress) + ", پلاک "
                    + (origin ? order.OriginalPlaque : order.DestinationPlaque),
                Coordinate(origin ? order.OriginLat : order.DestinationLat, -90, 90),
                Coordinate(origin ? order.OriginLng : order.DestinationLng, -180, 180),
                origin ? order.OriginalUnit : order.DestinationUnit, origin ? order.OriginPostalCode : order.DestinationPostalCode,
                origin ? order.SenderName + " " + order.SenderLastName : order.ReceiverName + " " + order.ReceiverLastName,
                origin ? order.SenderPhoneNumber : order.ReceiverPhoneNumber);
            // Logiwall's existing form stores kilograms and rials; do not apply ShopLink's grams conversion twice.
            parcels.Add(new(p.Id, Address(true), Address(false), p.Weight, p.ParcelValue, p.Length, p.Width, p.Height,
                packing.ExternalCode, content.ExternalCode, type, p.COD));
        }
        if (parcels.Count == 0) throw new BadRequestException("ShippingParcelInvalid");
        return new("logiwall-" + order.OrderNumber, parcels, 0, 0, 0, 0);
    }

    private static decimal Coordinate(string value, decimal minimum, decimal maximum) =>
        decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && number >= minimum && number <= maximum
            ? number : throw new BadRequestException("ShippingParcelInvalid");
}
