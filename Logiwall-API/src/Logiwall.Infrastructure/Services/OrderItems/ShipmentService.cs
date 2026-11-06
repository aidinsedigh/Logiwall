using System.Data;
using System.Globalization;
using Logiwall.Application.Abstractions.OrderItems;
using Logiwall.Application.Abstractions.Providers;
using Logiwall.Domain.Entities.OrderItems;
using Logiwall.Infrastructure.Data;
using Logiwall.Shared.Dtos.OrderItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Logiwall.Shared.Extensions;

namespace Logiwall.Infrastructure.Services.OrderItems;

public class ShipmentService(AppDbContext db, IEnumerable<IShippingCarrier> carriers,
    ILogger<ShipmentService> logger) : IShipmentService
{
    private static readonly TimeSpan UnmatchedEventRetryWindow = TimeSpan.FromDays(2);

    public async Task<ShipmentResponseDto> RegisterAsync(int orderId, int userId, CancellationToken cancellationToken)
    {
        Shipment shipment;
        ShipmentAttempt attempt;
        await using (var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken))
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT Id FROM Orders WITH (UPDLOCK,HOLDLOCK) WHERE Id={orderId}", cancellationToken);
            var order = await db.Orders.Include(o => o.OrderParcels)
                .SingleOrDefaultAsync(o => o.Id == orderId && o.Customer.Person.UserId == userId, cancellationToken)
                ?? throw new ResourceNotFoundException(nameof(AppStrings.InvalidOrder));
            var existing = await db.Shipments.Include(s => s.Parcels).Include(s => s.ServiceProvider)
                .SingleOrDefaultAsync(s => s.OrderId == orderId, cancellationToken);
            if (existing?.State == ShipmentRegistrationState.Registered) return Response(existing);
            // Only a definitive rejection or an edited draft can be submitted again by the customer.
            if (existing is not null && (existing.RegisteredAt is not null || existing.ExternalOrderId is not null
                || existing.State is not (ShipmentRegistrationState.Rejected or ShipmentRegistrationState.Prepared)))
                throw new ConflictException("ShippingOrderLocked");
            var quote = await db.OrderInqueryOptions.Include(q => q.ServiceProvider)
                .SingleOrDefaultAsync(q => q.OrderId == orderId && q.Choosen, cancellationToken)
                ?? throw new BadRequestException("ShippingQuoteRequired");
            if (quote.CarrierRequestJson is null || quote.ExternalServiceId is null)
                throw new BadRequestException("ShippingQuoteExpired");
            if (!Enum.IsDefined(quote.PaymentMethod)) throw new BadRequestException("ShippingPaymentMethodMismatch");
            var carrierSettled = quote.PaymentMethod != Constants.PaymentMethod.Online;
            if (carrierSettled && await db.OrderPayments.AnyAsync(p => p.OrderId == orderId && p.Status == PaymentStatus.Pending, cancellationToken))
                throw new ConflictException("ShippingRegistrationLocked");
            if (carrierSettled && (quote.ExpiresAt is null || quote.ExpiresAt <= DateTimeOffset.UtcNow))
                throw new BadRequestException("ShippingQuoteExpired");
            var selectedCarrier = carriers.SingleOrDefault(c => c.Code == quote.ServiceProvider.Code)
                ?? throw new BadRequestException("ShippingProviderUnavailable");
            if (!selectedCarrier.MatchesPaymentMethod(quote.CarrierRequestJson, quote.PaymentMethod))
                throw new BadRequestException("ShippingPaymentMethodMismatch");
            // Settlement follows the shared method frozen on the selected quote.
            if (!carrierSettled && !await db.OrderPayments.AnyAsync(p => p.OrderId == orderId && p.Status == PaymentStatus.Success
                && p.VerifiedAt != null && p.VerifiedAmount >= quote.FinalAmount, cancellationToken))
                throw new BadRequestException("ShippingPaymentRequired");
            if (!quote.ServiceProvider.IsActive || carriers.All(c => c.Code != quote.ServiceProvider.Code))
                throw new BadRequestException("ShippingProviderUnavailable");
            shipment = existing ?? new Shipment { OrderId = orderId,
                TraceCode = "logiwall-" + order.OrderNumber, CreatedAt = DateTimeOffset.UtcNow };
            if (existing is not null)
            {
                db.ShipmentParcels.RemoveRange(shipment.Parcels);
                // Release the unique position keys before inserting the updated parcel links.
                await db.SaveChangesAsync(cancellationToken);
            }
            shipment.QuoteId = quote.Id;
            shipment.Order = order;
            shipment.ServiceProviderId = quote.ServiceProviderId;
            shipment.ServiceProvider = quote.ServiceProvider;
            shipment.State = ShipmentRegistrationState.Submitting;
            order.OverallStatus = OrderStatusCode.Draft;
            shipment.RequestJson = quote.CarrierRequestJson;
            shipment.LastErrorCode = null;
            shipment.Parcels = order.OrderParcels.OrderBy(p => p.Id).Select((p, index) => new ShipmentParcel
                { OrderParcelId = p.Id, Position = index }).ToList();
            attempt = new ShipmentAttempt { Operation = "register", RequestJson = shipment.RequestJson, StartedAt = DateTimeOffset.UtcNow };
            shipment.Attempts.Add(attempt);
            if (existing is null) db.Shipments.Add(shipment);
            CarrierOrderStatus.Record(db, order, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        var carrier = carriers.Single(c => c.Code == shipment.ServiceProvider.Code);
        CarrierRegistrationResult result;
        // The request has committed. Client disconnection must not discard the carrier's reply.
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(70));
        try { result = await carrier.RegisterAsync(shipment.RequestJson, deadline.Token); }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            logger.LogWarning(e, "[Shipment][Register][{ShipmentId}] Registration outcome requires reconciliation.", shipment.Id);
            result = new(false, true, null, null, [], null, "RegistrationOutcomeUnknown");
        }
        using var persistenceDeadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        if (result.Succeeded && string.IsNullOrWhiteSpace(result.ExternalOrderId))
            result = result with { Succeeded = false, IsAmbiguous = true, ErrorCode = "IncompleteCarrierResponse" };
        Complete(attempt, result);
        shipment.State = result.Succeeded ? ShipmentRegistrationState.Registered
            : result.IsAmbiguous ? ShipmentRegistrationState.Unknown : ShipmentRegistrationState.Rejected;
        shipment.LastErrorCode = result.ErrorCode;
        shipment.Order.OverallStatus = OrderStatusCode.Draft;
        if (result.Succeeded)
        {
            shipment.ExternalOrderId = result.ExternalOrderId;
            shipment.RegisteredAt = DateTimeOffset.UtcNow;
            await CarrierOrderStatus.ApplyAsync(db, shipment, result.CarrierStatusCode, result.CarrierStatusTitle,
                shipment.RegisteredAt.Value, OrderStatusCode.Registered, persistenceDeadline.Token);
            var parcels = shipment.Parcels.OrderBy(p => p.Position).ToArray();
            if (result.TrackingCodes.Count == parcels.Length && result.TrackingCodes.Distinct().Count() == parcels.Length)
                for (var i = 0; i < parcels.Length; i++) parcels[i].TrackingCode = result.TrackingCodes[i];
            else shipment.LastErrorCode = "TrackingCodesRequireReconciliation";
        }
        CarrierOrderStatus.Record(db, shipment.Order, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(persistenceDeadline.Token);
        if (result.ErrorMessage is not null)
            throw new CarrierRequestException(result.ErrorMessage, result.IsAmbiguous ? 409 : result.HttpStatus is 401 or 403 ? 502 : result.HttpStatus ?? 400);
        if (result.ValidationErrors is not null)
            throw new ResourceValidationException("ShippingCarrierValidationFailed", result.ValidationErrors);
        logger.LogInformation("[Shipment][Register][{ShipmentId}][{State}] Registration finished.", shipment.Id, shipment.State);
        if (!result.Succeeded) throw new ConflictException(result.IsAmbiguous ? "ShippingOutcomeUnknown" : "ShippingRegistrationFailed");
        return Response(shipment);
    }

    public async Task<ShipmentResponseDto> GetAsync(int orderId, int userId, CancellationToken cancellationToken)
        => Response(await OwnedAsync(orderId, userId, cancellationToken));

    public async Task<ShipmentResponseDto> CancelAsync(int orderId, int userId, CancellationToken cancellationToken)
    {
        Shipment shipment;
        ShipmentAttempt attempt;
        await using (var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken))
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT Id FROM Orders WITH (UPDLOCK,HOLDLOCK) WHERE Id={orderId}", cancellationToken);
            shipment = await OwnedAsync(orderId, userId, cancellationToken);
            if (shipment.State == ShipmentRegistrationState.Cancelled) return Response(shipment);
            if (shipment.State != ShipmentRegistrationState.Registered || shipment.ExternalOrderId is null)
                throw new ConflictException("ShippingRegistrationLocked");
            // Claim cancellation without conflating order/payment state with carrier state.
            shipment.State = ShipmentRegistrationState.Submitting;
            attempt = new ShipmentAttempt { ShipmentId = shipment.Id, Operation = "cancel", StartedAt = DateTimeOffset.UtcNow };
            db.ShipmentAttempts.Add(attempt);
            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        CarrierRegistrationResult result;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(70));
        try { result = await carriers.Single(c => c.Code == shipment.ServiceProvider.Code).CancelAsync(shipment.ExternalOrderId!, deadline.Token); }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            logger.LogWarning(e, "[Shipment][Cancel][{ShipmentId}] Cancellation outcome requires reconciliation.", shipment.Id);
            result = new(false, true, null, null, [], null, "CancellationOutcomeUnknown");
        }
        Complete(attempt, result);
        shipment.State = result.Succeeded ? ShipmentRegistrationState.Cancelled
            : result.IsAmbiguous ? ShipmentRegistrationState.Unknown : ShipmentRegistrationState.Registered;
        shipment.LastErrorCode = result.ErrorCode;
        using var saveDeadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        if (result.Succeeded)
            await CarrierOrderStatus.ApplyAsync(db, shipment, result.CarrierStatusCode, result.CarrierStatusTitle,
                DateTimeOffset.UtcNow, OrderStatusCode.Cancelled, saveDeadline.Token);
        // An uncertain cancellation does not erase the last confirmed carrier stage.
        CarrierOrderStatus.Record(db, shipment.Order, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(saveDeadline.Token);
        if (result.ErrorMessage is not null)
            throw new CarrierRequestException(result.ErrorMessage, result.IsAmbiguous ? 409 : result.HttpStatus is 401 or 403 ? 502 : result.HttpStatus ?? 400);
        if (result.ValidationErrors is not null)
            throw new ResourceValidationException("ShippingCarrierValidationFailed", result.ValidationErrors);
        if (!result.Succeeded) throw new ConflictException(result.IsAmbiguous ? "ShippingOutcomeUnknown" : "ShippingCancellationFailed");
        return Response(shipment);
    }

    public async Task ReceiveEventAsync(CarrierParcelEventRequestDto request, CancellationToken cancellationToken)
    {
        var provider = await db.ServiceProviders.AsNoTracking().SingleAsync(p => p.Code == "etipax", cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        // Serialize carrier events to handle duplicate delivery and out-of-order arrival safely across instances.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT Id FROM ServiceProviders WITH (UPDLOCK,HOLDLOCK) WHERE Id={provider.Id}", cancellationToken);
        if (await db.ShipmentEvents.AnyAsync(e => e.ServiceProviderId == provider.Id && e.ExternalEventId == request.ParcelEventId, cancellationToken)) return;
        var item = new ShipmentEvent { ServiceProviderId = provider.Id, ExternalEventId = request.ParcelEventId,
            TrackingCode = request.Barcode, StatusCode = request.ParcelEventTypeId.ToString(CultureInfo.InvariantCulture),
            StatusTitle = request.EventTypeTitle, ReceivedAt = DateTimeOffset.UtcNow };
        db.ShipmentEvents.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        await ApplyEventAsync(item, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    public async Task ReconcileEventsAsync(CancellationToken cancellationToken)
    {
        var stale = DateTimeOffset.UtcNow.AddMinutes(-10);
        await db.Shipments.Where(s => s.State == ShipmentRegistrationState.Submitting
            && s.Attempts.All(a => a.StartedAt < stale))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.State, ShipmentRegistrationState.Unknown)
                .SetProperty(x => x.LastErrorCode, "InterruptedCarrierOperation"), cancellationToken);
        await db.Orders.Where(o => db.Shipments.Any(s => s.OrderId == o.Id && s.State == ShipmentRegistrationState.Unknown && s.RegisteredAt == null))
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.OverallStatus, OrderStatusCode.Draft), cancellationToken);
        // No carrier polling endpoint has been confirmed. Reconcile only actual received events.
        // An event still unmatched after the retry window stays stored for investigation but is not retried.
        var retryFrom = DateTimeOffset.UtcNow - UnmatchedEventRetryWindow;
        var ids = await db.ShipmentEvents.AsNoTracking().Where(e => !e.Processed && e.ReceivedAt > retryFrom)
            .OrderBy(e => e.LastReconciledAt).ThenBy(e => e.Id)
            .Select(e => e.Id).Take(100).ToListAsync(cancellationToken);
        foreach (var id in ids)
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var providerId = await db.ShipmentEvents.AsNoTracking().Where(e => e.Id == id).Select(e => e.ServiceProviderId).SingleAsync(cancellationToken);
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT Id FROM ServiceProviders WITH (UPDLOCK,HOLDLOCK) WHERE Id={providerId}", cancellationToken);
            var item = await db.ShipmentEvents.SingleAsync(e => e.Id == id, cancellationToken);
            await db.Entry(item).ReloadAsync(cancellationToken);
            if (!item.Processed) await ApplyEventAsync(item, cancellationToken);
            item.LastReconciledAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
    }

    private async Task ApplyEventAsync(ShipmentEvent item, CancellationToken ct)
    {
        var matches = await db.ShipmentParcels.Include(p => p.Shipment).ThenInclude(s => s.Order)
            .Include(p => p.Shipment).ThenInclude(s => s.Parcels).Where(p => p.TrackingCode == item.TrackingCode
            && p.Shipment.ServiceProviderId == item.ServiceProviderId).Take(2).ToListAsync(ct);
        if (matches.Count != 1) return;
        var parcel = matches[0];
        item.ShipmentParcelId = parcel.Id;
        item.Processed = true;
        if (parcel.LastExternalEventId >= item.ExternalEventId) return;
        parcel.LastExternalEventId = item.ExternalEventId;
        parcel.ExternalStatusCode = item.StatusCode;
        var reference = await db.ProviderReferenceItems.SingleOrDefaultAsync(r => r.ServiceProviderId == item.ServiceProviderId
            && r.Category == "status" && r.ExternalCode == item.StatusCode, ct);
        parcel.StatusTitle = item.StatusTitle ?? reference?.Title;
        parcel.MappedOrderStatus = reference?.MappedOrderStatus;
        parcel.LastStatusAt = item.ReceivedAt;
        parcel.Shipment.LastSyncedAt = item.ReceivedAt;
        item.Applied = true;
        parcel.Shipment.Order.OverallStatus = OrderStatusAggregation.FromParcels(parcel.Shipment.Parcels.Select(p => p.MappedOrderStatus), parcel.Shipment.Order.OverallStatus);
        db.OrderStatuses.Add(new OrderStatus { OrderId = parcel.Shipment.OrderId, ServiceProviderId = item.ServiceProviderId,
            ShipmentParcelId = parcel.Id, ShipmentEventId = item.Id, ExternalStatusCode = item.StatusCode,
            MappedOrderStatus = parcel.MappedOrderStatus, Status = (int)(parcel.MappedOrderStatus ?? parcel.Shipment.Order.OverallStatus),
            Description = parcel.StatusTitle, CreationDate = item.ReceivedAt });
    }

    private async Task<Shipment> OwnedAsync(int orderId, int userId, CancellationToken ct) =>
        await db.Shipments.Include(s => s.Parcels).Include(s => s.ServiceProvider).Include(s => s.Order)
            .SingleOrDefaultAsync(s => s.OrderId == orderId && s.Order.Customer.Person.UserId == userId, ct)
            ?? throw new ResourceNotFoundException(nameof(AppStrings.InvalidOrder));

    private static void Complete(ShipmentAttempt attempt, CarrierRegistrationResult result)
    {
        attempt.CompletedAt = DateTimeOffset.UtcNow;
        attempt.ResponseJson = result.ResponseJson;
        attempt.HttpStatusCode = result.HttpStatus;
        attempt.ErrorCode = result.ErrorCode;
    }
    private static ShipmentResponseDto Response(Shipment s) => new(s.Id, s.OrderId, s.ServiceProvider.Code == "etipax" ? "tipax" : s.ServiceProvider.Code!,
        s.State.ToString(), s.ExternalOrderId, s.RegisteredAt, s.Parcels.OrderBy(p => p.Position)
            .Select(p => new ShipmentParcelResponseDto(p.OrderParcelId, p.TrackingCode, p.ExternalStatusCode, p.StatusTitle)
            { MappedOrderStatus = (short?)p.MappedOrderStatus, MappedOrderStatusName = p.MappedOrderStatus?.GetDescription() }).ToArray())
    {
        StatusCode = s.Parcels.Select(p => p.ExternalStatusCode).Distinct().Count() == 1 ? s.Parcels.First().ExternalStatusCode : null,
        StatusTitle = s.Parcels.Select(p => p.ExternalStatusCode).Distinct().Count() == 1 ? s.Parcels.First().StatusTitle : null
    };
}
