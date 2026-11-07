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
        var selectedService = await db.ProviderReferenceItems.AsNoTracking().SingleOrDefaultAsync(r => r.Id == serviceReferenceId
            && r.Category == "service" && r.IsActive && r.ServiceProvider.IsActive, ct)
            ?? throw new BadRequestException("ShippingServiceSelectionRequired");
        var providers = await db.ServiceProviders.AsNoTracking().Where(p => p.IsActive && p.Code != null
            && p.Id == selectedService.ServiceProviderId).ToListAsync(ct);
        var newQuotes = new List<OrderInqueryOption>();
        foreach (var provider in providers)
        {
            var carrier = carriers.SingleOrDefault(x => x.Code == provider.Code);
            if (carrier is null) continue;
            var request = await BuildAsync(order, provider, ct);
            request = request with { PaymentType = (int)paymentMethod,
                ServiceId = int.Parse(selectedService.ExternalCode, CultureInfo.InvariantCulture) };
            try
            {
                foreach (var quote in await carrier.QuoteAsync(request, ct))
                {
                    if (quote.ServiceId != request.ServiceId) throw new JsonException("Carrier returned a different service.");
                    newQuotes.Add(new OrderInqueryOption { OrderId = orderId, ServiceProviderId = provider.Id,
                        ServiceAmount = quote.AmountRials, FinalAmount = quote.AmountRials,
                        CarrierRequestJson = quote.RequestJson, ExternalServiceId = quote.ServiceId,
                        PaymentMethod = paymentMethod,
                        ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15), DelivereDate = string.Empty });
                }
            }
            catch (Exception e) when (e is HttpRequestException or JsonException or InvalidOperationException
                || e is OperationCanceledException && !ct.IsCancellationRequested)
            { logger.LogWarning(e, "[CarrierQuote][Get][{OrderId}][{ProviderCode}] Carrier quotation failed.", orderId, provider.Code); }
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
        var titles = providers.ToDictionary(x => x.Id, x => x.Title);
        return new() { Succuss = true, Data = newQuotes.GroupBy(x => x.ServiceProviderId)
            .Select(g => new OrderProviderOptionsDto(g.Key, titles[g.Key], g.Select(q => new OrderInqueryOptionsDto
            { Id = q.Id, BaseAmount = q.ServiceAmount, TotalAmount = q.FinalAmount, DelivereDate = q.DelivereDate,
                PaymentType = q.PaymentMethod, ServiceTitle = selectedService.Title
            }).ToList())).ToList() };
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
