using System.Net;
using System.Net.Http.Json;
using Logiwall.Domain.Entities.OrderItems;
using Logiwall.IntegrationTests.Infrastructure;
using Logiwall.Shared.Enums;
using Logiwall.Infrastructure.Services.OrderItems;
using Logiwall.Application.Abstractions.Global;
using Logiwall.Application.Abstractions.OrderItems;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Logiwall.Shared.Enums.Constants;

namespace Logiwall.IntegrationTests.Orders;

[Collection("api")]
public class ShipmentTests(SqlServerFixture fixture)
{
    private async Task<(HttpClient Client, int OrderId, string Snapshot)> ArrangeAsync(bool paid = true, bool unknown = false)
    {
        var (client, customerId) = await OrderTestData.RegisteredUserAsync(fixture);
        var orderId = await OrderTestData.InsertOrderAsync(fixture, customerId);
        var snapshot = (unknown ? "unknown-" : "snapshot-") + Guid.NewGuid().ToString("N");
        await fixture.WithDbAsync(async db =>
        {
            var providerId = await db.ServiceProviders.Where(p => p.Code == "etipax").Select(p => p.Id).SingleAsync();
            db.OrderParcels.Add(new OrderParcel { OrderId = orderId, Weight = 2, ParcelValue = 4000000, Length = 10, Width = 10, Height = 10 });
            db.OrderInqueryOptions.Add(new OrderInqueryOption { OrderId = orderId, ServiceProviderId = providerId,
                Choosen = true, FinalAmount = 1000, CarrierRequestJson = snapshot, ExternalServiceId = 1,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15), DelivereDate = "" });
            if (paid) db.OrderPayments.Add(new OrderPayment { OrderId = orderId, Amount = 1000, VerifiedAmount = 1000,
                Status = PaymentStatus.Success, VerifiedAt = DateTimeOffset.UtcNow, PayTrackingCode = "test", PayDate = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
            return true;
        });
        return (client, orderId, snapshot);
    }

    [Fact]
    public async Task Successful_registration_updates_the_order_list_and_preserves_payment_history()
    {
        var (client, id, _) = await ArrangeAsync();
        (await client.PostAsJsonAsync("api/Shipment/Register", new { orderId = id })).EnsureSuccessStatusCode();
        var list = await ApiClient.ReadJsonAsync(await client.PostAsJsonAsync("api/Order/GetOrders", new { }));
        var order = list.GetProperty("data").GetProperty("result").EnumerateArray().Single(o => o.GetProperty("id").GetInt32() == id);
        Assert.Equal((short)OrderStatusCode.Registered, order.GetProperty("status").GetInt16());
        Assert.Equal("ثبت شده", order.GetProperty("statusName").GetString());
        Assert.Equal(1, await fixture.WithDbAsync(db => db.OrderPayments.CountAsync(p => p.OrderId == id && p.Status == PaymentStatus.Success)));
    }

    [Theory]
    [InlineData(30, OrderStatusCode.AwaitingPickup)]
    [InlineData(34, OrderStatusCode.Invalidated)]
    [InlineData(38, OrderStatusCode.Collected)]
    [InlineData(50, OrderStatusCode.Delivered)]
    [InlineData(51, OrderStatusCode.ReturningToSender)]
    [InlineData(52, OrderStatusCode.ReturningToPreviousNode)]
    [InlineData(57, OrderStatusCode.Cancelled)]
    [InlineData(58, OrderStatusCode.Processing)]
    [InlineData(59, OrderStatusCode.Registered)]
    [InlineData(60, OrderStatusCode.AtDestination)]
    [InlineData(61, OrderStatusCode.Registered)]
    [InlineData(62, OrderStatusCode.OriginTransit)]
    [InlineData(63, OrderStatusCode.ReturnedToSender)]
    [InlineData(9999, OrderStatusCode.Registered)]
    public async Task Carrier_status_is_preserved_and_mapped_into_the_general_order_status(int externalCode, OrderStatusCode mapped)
    {
        var (client, id, snapshot) = await ArrangeAsync();
        if (externalCode == 50)
            await fixture.WithDbAsync(async db =>
            {
                var other = new Logiwall.Domain.Entities.OrderItems.ServiceProvider { Name = "OtherCarrier", Title = "شرکت دیگر", Code = "other-status-test" };
                db.ServiceProviders.Add(other);
                db.ProviderReferenceItems.Add(new() { ServiceProvider = other, Category = "status", ExternalCode = "50",
                    Title = "تعریف متفاوت شرکت دیگر", IsActive = true, MappedOrderStatus = OrderStatusCode.Processing });
                await db.SaveChangesAsync(); return true;
            });
        (await client.PostAsJsonAsync("api/Shipment/Register", new { orderId = id })).EnsureSuccessStatusCode();
        using var webhook = fixture.Factory.CreateClient();
        webhook.DefaultRequestHeaders.Add("X-Tipax-Webhook-Secret", "integration-webhook-secret");
        (await webhook.PostAsJsonAsync("api/Shipment/Webhook", new
        {
            parcelEventId = DateTimeOffset.UtcNow.Ticks, parcelEventTypeId = externalCode,
            barcode = "barcode-" + snapshot, eventTypeTitle = "عنوان اصلی شرکت حمل"
        })).EnsureSuccessStatusCode();
        var shipment = (await ApiClient.ReadJsonAsync(await client.GetAsync($"api/Shipment/Get?orderId={id}"))).GetProperty("data");
        Assert.Equal(externalCode.ToString(), shipment.GetProperty("courierStatusCode").GetString());
        Assert.Equal("عنوان اصلی شرکت حمل", shipment.GetProperty("courierStatus").GetString());
        Assert.False(shipment.TryGetProperty("statusTitle", out _));
        var parcel = shipment.GetProperty("parcels")[0];
        Assert.Equal(externalCode.ToString(), parcel.GetProperty("courierStatusCode").GetString());
        Assert.Equal("عنوان اصلی شرکت حمل", parcel.GetProperty("courierStatus").GetString());
        Assert.Equal("Registered", shipment.GetProperty("operationState").GetString());
        await fixture.WithDbAsync(async db =>
        {
            Assert.Equal(mapped, await db.Orders.Where(o => o.Id == id).Select(o => o.OverallStatus).SingleAsync());
            var history = await db.OrderStatuses.SingleAsync(s => s.OrderId == id && s.ShipmentEventId != null);
            Assert.Equal(externalCode.ToString(), history.ExternalStatusCode);
            Assert.Equal(externalCode == 9999 ? null : (OrderStatusCode?)mapped, history.MappedOrderStatus);
            return true;
        });
    }

    [Fact]
    public async Task Status_catalogue_is_general_and_reference_sync_preserves_the_carrier_mapping()
    {
        using var anonymous = fixture.Factory.CreateClient();
        var catalogue = (await ApiClient.ReadJsonAsync(await anonymous.GetAsync("api/Common/GetOrderStatuses"))).GetProperty("data");
        Assert.Contains(catalogue.EnumerateArray(), s => s.GetProperty("code").GetString() == "Delivered" && s.GetProperty("id").GetInt16() == 20);
        Assert.DoesNotContain(catalogue.EnumerateArray(), s => new[] { 1, 2, 3, 4, 11, 23, 24 }.Contains(s.GetProperty("id").GetInt16()));
        using var scope = fixture.Factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IShippingReferenceService>().SynchronizeAsync(CancellationToken.None);
        await fixture.WithDbAsync(async db =>
        {
            var definition = await db.ProviderReferenceItems.SingleAsync(r => r.ServiceProvider.Code == "etipax" && r.Category == "status" && r.ExternalCode == "50");
            Assert.Equal(OrderStatusCode.Delivered, definition.MappedOrderStatus);
            return true;
        });
    }

    [Fact]
    public async Task Multiple_parcels_are_not_delivered_until_every_parcel_is_delivered()
    {
        var (client, id, snapshot) = await ArrangeAsync();
        (await client.PostAsJsonAsync("api/Shipment/Register", new { orderId = id })).EnsureSuccessStatusCode();
        await fixture.WithDbAsync(async db =>
        {
            var parcel = await db.OrderParcels.SingleAsync(p => p.OrderId == id);
            var shipment = await db.Shipments.SingleAsync(s => s.OrderId == id);
            db.ShipmentParcels.Add(new() { ShipmentId = shipment.Id, OrderParcelId = parcel.Id, Position = 1,
                TrackingCode = "second-" + snapshot, MappedOrderStatus = OrderStatusCode.Registered });
            await db.SaveChangesAsync(); return true;
        });
        using var webhook = fixture.Factory.CreateClient();
        webhook.DefaultRequestHeaders.Add("X-Tipax-Webhook-Secret", "integration-webhook-secret");
        foreach (var barcode in new[] { "barcode-" + snapshot, "second-" + snapshot })
        {
            (await webhook.PostAsJsonAsync("api/Shipment/Webhook", new { parcelEventId = DateTimeOffset.UtcNow.Ticks, parcelEventTypeId = 50, barcode })).EnsureSuccessStatusCode();
            var status = await fixture.WithDbAsync(db => db.Orders.Where(o => o.Id == id).Select(o => o.OverallStatus).SingleAsync());
            Assert.Equal(barcode.StartsWith("second-") ? OrderStatusCode.Delivered : OrderStatusCode.Registered, status);
        }
    }

    [Fact]
    public async Task Parallel_registration_sends_exactly_one_carrier_request()
    {
        var (client, id, snapshot) = await ArrangeAsync();
        var responses = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => client.PostAsJsonAsync("api/Shipment/Register", new { orderId = id })));
        Assert.Contains(responses, r => r.IsSuccessStatusCode);
        Assert.Equal(1, fixture.Factory.Services.GetRequiredService<FakeShippingCarrier>().Calls[snapshot]);
        var result = await ApiClient.ReadJsonAsync(await client.GetAsync($"api/Shipment/Get?orderId={id}"));
        Assert.Equal("Registered", result.GetProperty("data").GetProperty("state").GetString());
        Assert.Equal("tipax", result.GetProperty("data").GetProperty("providerCode").GetString());
        var parcel = result.GetProperty("data").GetProperty("parcels")[0];
        Assert.Equal("barcode-" + snapshot, parcel.GetProperty("courierTrackingCode").GetString());
        Assert.False(parcel.TryGetProperty("trackingCode", out _));
    }

    [Fact]
    public async Task Unknown_outcome_is_saved_and_never_retried_automatically()
    {
        var (client, id, snapshot) = await ArrangeAsync(unknown: true);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("api/Shipment/Register", new { orderId = id })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("api/Shipment/Register", new { orderId = id })).StatusCode);
        Assert.Equal(1, fixture.Factory.Services.GetRequiredService<FakeShippingCarrier>().Calls[snapshot]);
        Assert.Equal(ShipmentRegistrationState.Unknown, await fixture.WithDbAsync(db => db.Shipments.Where(s => s.OrderId == id).Select(s => s.State).SingleAsync()));
    }

    [Fact]
    public async Task Unverified_payment_prevents_shipping()
    {
        var (client, id, snapshot) = await ArrangeAsync(paid: false);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("api/Shipment/Register", new { orderId = id })).StatusCode);
        Assert.False(fixture.Factory.Services.GetRequiredService<FakeShippingCarrier>().Calls.ContainsKey(snapshot));
    }

    [Fact]
    public async Task Another_customer_cannot_register_or_read_shipment()
    {
        var (_, id, snapshot) = await ArrangeAsync();
        var (other, _) = await OrderTestData.RegisteredUserAsync(fixture);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsJsonAsync("api/Shipment/Register", new { orderId = id })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"api/Shipment/Get?orderId={id}")).StatusCode);
        Assert.False(fixture.Factory.Services.GetRequiredService<FakeShippingCarrier>().Calls.ContainsKey(snapshot));
    }

    [Fact]
    public async Task Webhook_requires_secret_deduplicates_and_does_not_roll_back_status()
    {
        var (client, id, snapshot) = await ArrangeAsync();
        (await client.PostAsJsonAsync("api/Shipment/Register", new { orderId = id })).EnsureSuccessStatusCode();
        var eventId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var body = new { parcelEventId = eventId, parcelEventTypeId = 5, barcode = "barcode-" + snapshot, eventTypeTitle = "Delivered" };
        using var webhook = fixture.Factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await webhook.PostAsJsonAsync("api/Shipment/Webhook", body)).StatusCode);
        webhook.DefaultRequestHeaders.Add("X-Tipax-Webhook-Secret", "integration-webhook-secret");
        (await webhook.PostAsJsonAsync("api/Shipment/Webhook", body)).EnsureSuccessStatusCode();
        (await webhook.PostAsJsonAsync("api/Shipment/Webhook", body)).EnsureSuccessStatusCode();
        (await webhook.PostAsJsonAsync("api/Shipment/Webhook", new { parcelEventId = eventId - 1, parcelEventTypeId = 2, body.barcode, eventTypeTitle = "Old" })).EnsureSuccessStatusCode();
        await fixture.WithDbAsync(async db =>
        {
            Assert.Equal("5", await db.ShipmentParcels.Where(p => p.Shipment.OrderId == id).Select(p => p.ExternalStatusCode).SingleAsync());
            Assert.Equal(1, await db.OrderStatuses.CountAsync(s => s.OrderId == id && s.ShipmentEventId != null));
            Assert.Equal(2, await db.ShipmentEvents.CountAsync(e => e.TrackingCode == body.barcode));
            return true;
        });
    }

    [Fact]
    public async Task Migration_imports_separate_etipax_city_codes()
    {
        await fixture.WithDbAsync(async db =>
        {
            Assert.Equal(1356, await db.StaticDataMappers.CountAsync(m => m.ProviderType == ProviderType.ETipax));
            Assert.Equal(2, await db.ShippingJobLeases.CountAsync());
            Assert.False(await db.ServiceProviders.Where(p => p.Name == "MyTipaxProvider").Select(p => p.IsActive).SingleAsync());
            return true;
        });
    }

    [Fact]
    public async Task Unmatched_webhook_is_applied_after_registration_by_reconciliation_job()
    {
        var (client, id, snapshot) = await ArrangeAsync();
        using var webhook = fixture.Factory.CreateClient();
        webhook.DefaultRequestHeaders.Add("X-Tipax-Webhook-Secret", "integration-webhook-secret");
        var barcode = "barcode-" + snapshot;
        (await webhook.PostAsJsonAsync("api/Shipment/Webhook", new { parcelEventId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), parcelEventTypeId = 3, barcode })).EnsureSuccessStatusCode();
        Assert.False(await fixture.WithDbAsync(db => db.ShipmentEvents.Where(e => e.TrackingCode == barcode).Select(e => e.Processed).SingleAsync()));
        (await client.PostAsJsonAsync("api/Shipment/Register", new { orderId = id })).EnsureSuccessStatusCode();
        using var scope = fixture.Factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ShipmentService>().ReconcileEventsAsync(CancellationToken.None);
        Assert.True(await fixture.WithDbAsync(db => db.ShipmentEvents.Where(e => e.TrackingCode == barcode).Select(e => e.Applied).SingleAsync()));
    }

    [Fact]
    public async Task Unmatched_webhook_older_than_the_retry_window_is_kept_but_not_retried()
    {
        using var webhook = fixture.Factory.CreateClient();
        webhook.DefaultRequestHeaders.Add("X-Tipax-Webhook-Secret", "integration-webhook-secret");
        var barcode = "foreign-" + Guid.NewGuid().ToString("N");
        (await webhook.PostAsJsonAsync("api/Shipment/Webhook", new { parcelEventId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), parcelEventTypeId = 3, barcode })).EnsureSuccessStatusCode();
        await fixture.WithDbAsync(db => db.ShipmentEvents.Where(e => e.TrackingCode == barcode)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.ReceivedAt, DateTimeOffset.UtcNow.AddDays(-3))));

        using var scope = fixture.Factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ShipmentService>().ReconcileEventsAsync(CancellationToken.None);

        var stored = await fixture.WithDbAsync(db => db.ShipmentEvents.Where(e => e.TrackingCode == barcode)
            .Select(e => new { e.Processed, e.LastReconciledAt }).SingleAsync());
        Assert.False(stored.Processed);
        Assert.Null(stored.LastReconciledAt);
    }

    [Fact]
    public async Task Reference_sync_preserves_internal_ids_and_scopes_categories()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var references = scope.ServiceProvider.GetRequiredService<IShippingReferenceService>();
        await references.SynchronizeAsync(CancellationToken.None);
        var first = await references.GetAsync("etipax", "packaging", CancellationToken.None);
        await references.SynchronizeAsync(CancellationToken.None);
        var second = await references.GetAsync("etipax", "packaging", CancellationToken.None);
        Assert.Single(first);
        Assert.Equal(first[0].Id, second[0].Id);
        Assert.Equal(1, second[0].PackType);
        Assert.NotEqual(first[0].Id, (await references.GetAsync("etipax", "content", CancellationToken.None))[0].Id);
    }

    [Fact]
    public async Task Cancellation_keeps_carrier_state_separate_from_payment()
    {
        var (client, id, _) = await ArrangeAsync();
        (await client.PostAsJsonAsync("api/Shipment/Register", new { orderId = id })).EnsureSuccessStatusCode();
        var response = await ApiClient.ReadJsonAsync(await client.PostAsJsonAsync("api/Shipment/Cancel", new { orderId = id }));
        Assert.Equal("Cancelled", response.GetProperty("data").GetProperty("state").GetString());
        (await client.PostAsJsonAsync("api/Shipment/Cancel", new { orderId = id })).EnsureSuccessStatusCode();
        Assert.Equal(PaymentStatus.Success, await fixture.WithDbAsync(db => db.OrderPayments.Where(p => p.OrderId == id).Select(p => p.Status).SingleAsync()));
    }

    [Theory]
    [InlineData(20, true)]
    [InlineData(10, false)]
    public async Task Carrier_collection_requires_matching_snapshot_payment_mode(int snapshotMode, bool accepted)
    {
        var (_, id, _) = await ArrangeAsync(paid: false);
        await fixture.WithDbAsync(async db =>
        {
            var quote = await db.OrderInqueryOptions.SingleAsync(q => q.OrderId == id);
            quote.CarrierRequestJson = "{\"packages\":[{\"paymentType\":" + snapshotMode + "}]}";
            quote.PaymentMethod = PaymentMethod.CashOnDelivery;
            await db.SaveChangesAsync();
            var userId = await db.Orders.Where(o => o.Id == id).Select(o => o.Customer.Person.UserId).SingleAsync();
            var service = new ShipmentService(db, [new FakeShippingCarrier()], NullLogger<ShipmentService>.Instance);
            if (accepted) Assert.Equal("Registered", (await service.RegisterAsync(id, userId, CancellationToken.None)).State);
            else await Assert.ThrowsAsync<Logiwall.Shared.Exceptions.BadRequestException>(() => service.RegisterAsync(id, userId, CancellationToken.None));
            return true;
        });
    }

    [Fact]
    public async Task Carrier_quote_uses_internal_selections_and_freezes_registration_request()
    {
        var (_, id, _) = await ArrangeAsync(paid: false);
        using var scope = fixture.Factory.Services.CreateScope();
        var references = scope.ServiceProvider.GetRequiredService<IShippingReferenceService>();
        await references.SynchronizeAsync(CancellationToken.None);
        var packaging = (await references.GetAsync("etipax", "packaging", CancellationToken.None))[0].Id;
        var content = (await references.GetAsync("etipax", "content", CancellationToken.None))[0].Id;
        var userId = await fixture.WithDbAsync(async db =>
        {
            var order = await db.Orders.Include(o => o.OrderParcels).SingleAsync(o => o.Id == id);
            order.OriginCity = (short)await db.StaticDataMappers.Where(m => m.ProviderType == ProviderType.ETipax && m.StaticData!.Type == StaticDataType.OriginCity).Select(m => m.StaticDataId).FirstAsync();
            order.DestinationCity = (short)await db.StaticDataMappers.Where(m => m.ProviderType == ProviderType.ETipax && m.StaticData!.Type == StaticDataType.DestinationCity).Select(m => m.StaticDataId).FirstAsync();
            order.OrderParcels.Single().PackagingReferenceId = packaging;
            order.OrderParcels.Single().ContentReferenceId = content;
            await db.SaveChangesAsync();
            return await db.Orders.Where(o => o.Id == id).Select(o => o.Customer.Person.UserId).SingleAsync();
        });
        var serviceId = await fixture.WithDbAsync(db => db.ProviderReferenceItems.Where(r => r.Category == "service" && r.ExternalCode == "4").Select(r => r.Id).SingleAsync());
        var result = await scope.ServiceProvider.GetRequiredService<ICarrierQuoteService>().GetAsync(id, userId, CancellationToken.None, serviceReferenceId: serviceId);
        Assert.True(result.Succuss);
        Assert.Single(result.Data!.Single().OrderOptions);
        await fixture.WithDbAsync(async db =>
        {
            var quote = await db.OrderInqueryOptions.SingleAsync(q => q.OrderId == id);
            Assert.StartsWith("logiwall-", quote.CarrierRequestJson);
            Assert.Equal(4, quote.ExternalServiceId);
            Assert.True(quote.ExpiresAt > DateTimeOffset.UtcNow);
            Assert.False(quote.Choosen);
            Assert.Equal(PaymentMethod.CashOnDelivery, quote.PaymentMethod);
            return true;
        });
        await fixture.WithDbAsync(async db =>
        {
            (await db.OrderParcels.SingleAsync(p => p.OrderId == id)).PackagingReferenceId = null;
            await db.SaveChangesAsync();
            return true;
        });
        using var freshScope = fixture.Factory.Services.CreateScope();
        var error = await Assert.ThrowsAsync<Logiwall.Shared.Exceptions.ResourceValidationException>(() =>
            freshScope.ServiceProvider.GetRequiredService<ICarrierQuoteService>().GetAsync(id, userId, CancellationToken.None, serviceReferenceId: serviceId));
        Assert.Equal("ShippingPackagingRequired", error.Key);
        Assert.Equal("orderParcels[0].packagingReferenceId", error.Payload.Details.Single().Name);
    }

    [Fact]
    public async Task Shared_payment_choices_and_provider_service_catalog_are_separate()
    {
        using var anonymous = fixture.Factory.CreateClient();
        var methods = await ApiClient.ReadJsonAsync(await anonymous.GetAsync("api/Common/GetPaymentMethods"));
        Assert.Equal(Enumerable.Range(71, 8), methods.GetProperty("data").EnumerateArray().Select(x => x.GetProperty("id").GetInt32()));
        var (client, _, _) = await ArrangeAsync();
        var services = await ApiClient.ReadJsonAsync(await client.GetAsync("api/Shipment/Services?providerCode=tipax"));
        Assert.Equal(13, services.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public async Task An_online_payment_cannot_charge_a_carrier_collection_quote()
    {
        var (client, id, _) = await ArrangeAsync(paid: false);
        var optionId = await fixture.WithDbAsync(async db =>
        {
            var quote = await db.OrderInqueryOptions.SingleAsync(q => q.OrderId == id);
            quote.PaymentMethod = PaymentMethod.CashOnDelivery;
            await db.SaveChangesAsync();
            return quote.Id;
        });
        Assert.Equal(HttpStatusCode.BadRequest, (await OrderTestData.PayAsync(client, id, optionId)).StatusCode);
        Assert.Equal(0, await fixture.WithDbAsync(db => db.OrderPayments.CountAsync(p => p.OrderId == id)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(999999)]
    public async Task Pricing_requires_a_selected_active_service(int? serviceId)
    {
        var (_, id, _) = await ArrangeAsync(paid: false);
        using var scope = fixture.Factory.Services.CreateScope();
        var userId = await fixture.WithDbAsync(db => db.Orders.Where(o => o.Id == id).Select(o => o.Customer.Person.UserId).SingleAsync());
        var error = await Assert.ThrowsAsync<Logiwall.Shared.Exceptions.BadRequestException>(() =>
            scope.ServiceProvider.GetRequiredService<ICarrierQuoteService>().GetAsync(id, userId, CancellationToken.None, serviceReferenceId: serviceId));
        Assert.Equal("ShippingServiceSelectionRequired", error.Key);
    }

    [Theory]
    [InlineData(73)]
    [InlineData(74)]
    [InlineData(75)]
    [InlineData(76)]
    [InlineData(77)]
    [InlineData(78)]
    public async Task All_carrier_settled_methods_register_once_with_the_selected_mapping(int method)
    {
        var (client, id, snapshot) = await ArrangeAsync(paid: false);
        var externalCode = method switch { 73 => 30, 74 => 40, 75 => 50, 76 => 60, 77 => 80, 78 => 90, _ => throw new InvalidOperationException() };
        snapshot = System.Text.Json.JsonSerializer.Serialize(new { packages = new[] { new { paymentType = externalCode } }, traceCode = id.ToString() });
        await fixture.WithDbAsync(async db =>
        {
            var quote = await db.OrderInqueryOptions.SingleAsync(q => q.OrderId == id);
            quote.PaymentMethod = (PaymentMethod)method;
            quote.CarrierRequestJson = snapshot;
            await db.SaveChangesAsync();
            return true;
        });
        var response = await client.PostAsJsonAsync("api/Shipment/Register", new { orderId = id });
        response.EnsureSuccessStatusCode();
        Assert.Equal("Registered", (await ApiClient.ReadJsonAsync(response)).GetProperty("data").GetProperty("state").GetString());
        (await client.PostAsJsonAsync("api/Shipment/Register", new { orderId = id })).EnsureSuccessStatusCode();
        Assert.Equal(1, fixture.Factory.Services.GetRequiredService<FakeShippingCarrier>().Calls[snapshot]);
        Assert.True(await fixture.WithDbAsync(db => db.Shipments.AnyAsync(s => s.OrderId == id && s.State == ShipmentRegistrationState.Registered)));
        Assert.Equal(0, await fixture.WithDbAsync(db => db.OrderPayments.CountAsync(p => p.OrderId == id)));
    }
}
