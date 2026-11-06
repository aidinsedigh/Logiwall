using System.Net;
using System.Net.Http.Json;
using System.Collections.Concurrent;
using Logiwall.Application.Abstractions.Global;
using Logiwall.Application.Abstractions.OrderItems;
using Logiwall.Application.Abstractions.Providers;
using Logiwall.Domain.Entities.OrderItems;
using Logiwall.IntegrationTests.Infrastructure;
using Logiwall.Shared.Dtos.OrderItems;
using Logiwall.Shared.Enums;
using Logiwall.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Logiwall.Shared.Enums.Constants;

namespace Logiwall.IntegrationTests.Orders;

[Collection("api")]
public class OrderEditTests(SqlServerFixture fixture)
{
    private async Task<(HttpClient Client, UpdateOrderRequestDto Form, int UserId, int ServiceId, string Trace)> ArrangeAsync()
    {
        var (client, customerId) = await OrderTestData.RegisteredUserAsync(fixture);
        using var scope = fixture.Factory.Services.CreateScope();
        var references = scope.ServiceProvider.GetRequiredService<IShippingReferenceService>();
        await references.SynchronizeAsync(CancellationToken.None);
        var packaging = (await references.GetAsync("etipax", "packaging", CancellationToken.None))[0].Id;
        var content = (await references.GetAsync("etipax", "content", CancellationToken.None))[0].Id;
        var form = new UpdateOrderRequestDto
        {
            ChannelId = 2, SenderName = "Sender", SenderLastName = "Test", SenderPhoneNumber = "09123456789",
            OriginAddress = "Origin", OriginLat = "35.7", OriginLng = "51.4", OriginalPlaque = "1", OriginalUnit = "2",
            ReceiverName = "Receiver", ReceiverLastName = "Test", ReceiverPhoneNumber = "09123456780",
            DestinationAddress = "Destination", DestinationLat = "37.2", DestinationLng = "49.5", DestinationPlaque = "3", DestinationUnit = "4",
            OrderParcels = [new() { ParcelTypeId = ParcelType.Package, Weight = 1, Length = 10, Width = 10, Height = 10,
                ParcelValue = 100000, PackagingReferenceId = packaging, ContentReferenceId = content }]
        };
        var setup = await fixture.WithDbAsync(async db =>
        {
            form.OriginCity = (short)await db.StaticDataMappers.Where(m => m.ProviderType == ProviderType.ETipax && m.StaticData!.Type == StaticDataType.OriginCity).Select(m => m.StaticDataId).FirstAsync();
            form.DestinationCity = (short)await db.StaticDataMappers.Where(m => m.ProviderType == ProviderType.ETipax && m.StaticData!.Type == StaticDataType.DestinationCity).Select(m => m.StaticDataId).FirstAsync();
            return (UserId: await db.Customers.Where(c => c.Id == customerId).Select(c => c.Person.UserId).SingleAsync(),
                ServiceId: await db.ProviderReferenceItems.Where(r => r.Category == "service" && r.ExternalCode == "4").Select(r => r.Id).SingleAsync());
        });
        var created = await client.PostAsJsonAsync("api/Order/CreateOrder", form);
        created.EnsureSuccessStatusCode();
        var data = (await ApiClient.ReadJsonAsync(created)).GetProperty("data");
        form.OrderId = data.GetProperty("orderId").GetInt32();
        return (client, form, setup.UserId, setup.ServiceId, "logiwall-" + data.GetProperty("orderNumber").GetString());
    }

    private async Task<int> QuoteAsync(int orderId, int userId, int serviceId)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var response = await scope.ServiceProvider.GetRequiredService<ICarrierQuoteService>().GetAsync(orderId, userId,
            CancellationToken.None, PaymentMethod.CashOnDelivery, serviceId);
        Assert.True(response.Succuss);
        return response.Data!.Single().OrderOptions.Single().Id;
    }

    [Fact]
    public async Task Edit_keeps_identity_replaces_parcels_and_invalidates_old_prices()
    {
        var (client, form, userId, serviceId, _) = await ArrangeAsync();
        var quote = await QuoteAsync(form.OrderId, userId, serviceId);
        (await OrderTestData.SelectAsync(client, form.OrderId, quote)).EnsureSuccessStatusCode();
        form.SenderName = "Edited sender";
        form.DestinationAddress = "Edited destination";
        form.OrderParcels.Add(form.OrderParcels[0] with { Weight = 2 });
        var response = await client.PostAsJsonAsync("api/Order/UpdateOrder", form);
        response.EnsureSuccessStatusCode();
        Assert.Equal(form.OrderId, (await ApiClient.ReadJsonAsync(response)).GetProperty("data").GetProperty("orderId").GetInt32());
        await fixture.WithDbAsync(async db =>
        {
            var order = await db.Orders.Include(o => o.OrderParcels).SingleAsync(o => o.Id == form.OrderId);
            Assert.Equal(form.SenderName, order.SenderName);
            Assert.Equal(form.DestinationAddress, order.DestinationAddress);
            Assert.Equal(2, order.OrderParcels.Count);
            Assert.Equal(0, order.ServiceAmount);
            Assert.False(await db.OrderInqueryOptions.AnyAsync(q => q.OrderId == form.OrderId));
            return true;
        });
        Assert.Equal(HttpStatusCode.BadRequest, (await OrderTestData.SelectAsync(client, form.OrderId, quote)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("api/Shipment/Register", new { form.OrderId })).StatusCode);
    }

    [Fact]
    public async Task Owner_can_edit_after_rejection_requote_repeatedly_and_register_same_order()
    {
        var (client, form, userId, serviceId, trace) = await ArrangeAsync();
        var carrier = fixture.Factory.Services.GetRequiredService<FakeShippingCarrier>();
        carrier.Results[trace] = new ConcurrentQueue<CarrierRegistrationResult>([
            new(false, false, 422, null, [], "{}", "CarrierRejected", ErrorMessage: "آدرس نامعتبر است")]);
        var quote = await QuoteAsync(form.OrderId, userId, serviceId);
        (await OrderTestData.SelectAsync(client, form.OrderId, quote)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.PostAsJsonAsync("api/Shipment/Register", new { form.OrderId })).StatusCode);
        form.DestinationAddress = "Corrected address";
        (await client.PostAsJsonAsync("api/Order/UpdateOrder", form)).EnsureSuccessStatusCode();
        var first = await QuoteAsync(form.OrderId, userId, serviceId);
        quote = await QuoteAsync(form.OrderId, userId, serviceId);
        Assert.NotEqual(first, quote);
        (await OrderTestData.SelectAsync(client, form.OrderId, quote)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("api/Shipment/Register", new { form.OrderId })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("api/Shipment/Register", new { form.OrderId })).EnsureSuccessStatusCode();
        Assert.Equal(2, carrier.Calls[trace]);
        await fixture.WithDbAsync(async db =>
        {
            var shipment = await db.Shipments.Include(s => s.Attempts).SingleAsync(s => s.OrderId == form.OrderId);
            Assert.Equal(ShipmentRegistrationState.Registered, shipment.State);
            Assert.Equal(2, shipment.Attempts.Count);
            Assert.Single(await db.ShipmentParcels.Where(p => p.ShipmentId == shipment.Id).ToListAsync());
            Assert.False(await db.OrderPayments.AnyAsync(p => p.OrderId == form.OrderId));
            return true;
        });
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("api/Order/UpdateOrder", form)).StatusCode);
        await Assert.ThrowsAsync<ConflictException>(() => QuoteAsync(form.OrderId, userId, serviceId));
    }

    [Fact]
    public async Task Explicit_parallel_retry_after_definitive_rejection_sends_only_one_new_request()
    {
        var (client, form, userId, serviceId, trace) = await ArrangeAsync();
        var carrier = fixture.Factory.Services.GetRequiredService<FakeShippingCarrier>();
        carrier.Results[trace] = new ConcurrentQueue<CarrierRegistrationResult>([
            new(false, false, 400, null, [], "{}", "CarrierRejected", ErrorMessage: "خطای ثبت")]);
        var quote = await QuoteAsync(form.OrderId, userId, serviceId);
        (await OrderTestData.SelectAsync(client, form.OrderId, quote)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("api/Shipment/Register", new { form.OrderId })).StatusCode);
        var retries = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => client.PostAsJsonAsync("api/Shipment/Register", new { form.OrderId })));
        Assert.Contains(retries, r => r.IsSuccessStatusCode);
        Assert.Equal(2, carrier.Calls[trace]);
        Assert.Equal(2, await fixture.WithDbAsync(db => db.ShipmentAttempts.CountAsync(a => a.Shipment.OrderId == form.OrderId)));
    }

    [Fact]
    public async Task A_success_flag_without_a_carrier_order_id_does_not_finalize_the_order()
    {
        var (client, form, userId, serviceId, trace) = await ArrangeAsync();
        var carrier = fixture.Factory.Services.GetRequiredService<FakeShippingCarrier>();
        carrier.Results[trace] = new ConcurrentQueue<CarrierRegistrationResult>([new(true, false, 200, null, [], "{}", null)]);
        var quote = await QuoteAsync(form.OrderId, userId, serviceId);
        (await OrderTestData.SelectAsync(client, form.OrderId, quote)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("api/Shipment/Register", new { form.OrderId })).StatusCode);
        await fixture.WithDbAsync(async db =>
        {
            var shipment = await db.Shipments.SingleAsync(s => s.OrderId == form.OrderId);
            Assert.Equal(ShipmentRegistrationState.Unknown, shipment.State);
            Assert.Null(shipment.RegisteredAt);
            return true;
        });
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("api/Shipment/Register", new { form.OrderId })).StatusCode);
        Assert.Equal(1, carrier.Calls[trace]);
    }

    [Fact]
    public async Task Editing_requires_ownership_and_the_complete_validated_form()
    {
        var (client, form, _, _, _) = await ArrangeAsync();
        var (other, _) = await OrderTestData.RegisteredUserAsync(fixture);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsJsonAsync("api/Order/UpdateOrder", form)).StatusCode);
        form.OrderParcels = [];
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("api/Order/UpdateOrder", form)).StatusCode);
    }

    [Theory]
    [InlineData(ShipmentRegistrationState.Submitting)]
    [InlineData(ShipmentRegistrationState.Unknown)]
    [InlineData(ShipmentRegistrationState.Cancelled)]
    public async Task Uncertain_or_previously_registered_orders_cannot_be_edited_or_requoted(ShipmentRegistrationState state)
    {
        var (client, form, userId, serviceId, trace) = await ArrangeAsync();
        await fixture.WithDbAsync(async db =>
        {
            var providerId = await db.ServiceProviders.Where(p => p.Code == "etipax").Select(p => p.Id).SingleAsync();
            db.Shipments.Add(new() { OrderId = form.OrderId, ServiceProviderId = providerId, TraceCode = trace,
                State = state, CreatedAt = DateTimeOffset.UtcNow, RegisteredAt = state == ShipmentRegistrationState.Cancelled ? DateTimeOffset.UtcNow : null });
            await db.SaveChangesAsync();
            return true;
        });
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("api/Order/UpdateOrder", form)).StatusCode);
        await Assert.ThrowsAsync<ConflictException>(() => QuoteAsync(form.OrderId, userId, serviceId));
    }

    [Theory]
    [InlineData(OrderState.Paid)]
    [InlineData(OrderState.PaymentPending)]
    public async Task Payment_state_does_not_finalize_shipping_or_block_draft_editing(OrderState state)
    {
        var (client, form, userId, serviceId, _) = await ArrangeAsync();
        await fixture.WithDbAsync(async db => { (await db.Orders.SingleAsync(o => o.Id == form.OrderId)).State = (short)state; await db.SaveChangesAsync(); return true; });
        await QuoteAsync(form.OrderId, userId, serviceId);
        (await client.PostAsJsonAsync("api/Order/UpdateOrder", form)).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task A_price_for_the_old_form_cannot_be_saved_after_an_edit()
    {
        var (client, form, userId, serviceId, trace) = await ArrangeAsync();
        var carrier = fixture.Factory.Services.GetRequiredService<FakeShippingCarrier>();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        carrier.QuoteGates[trace] = (started, release);
        try
        {
            var pricing = QuoteAsync(form.OrderId, userId, serviceId);
            await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
            form.SenderName = "Changed while pricing";
            (await client.PostAsJsonAsync("api/Order/UpdateOrder", form)).EnsureSuccessStatusCode();
            release.TrySetResult();
            await Assert.ThrowsAsync<ConflictException>(() => pricing);
            Assert.False(await fixture.WithDbAsync(db => db.OrderInqueryOptions.AnyAsync(q => q.OrderId == form.OrderId)));
        }
        finally { release.TrySetResult(); carrier.QuoteGates.TryRemove(trace, out _); }
    }
}

