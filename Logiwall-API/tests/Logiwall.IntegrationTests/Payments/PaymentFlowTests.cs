using System.Net;
using System.Net.Http.Json;
using Logiwall.Domain.Entities.OrderItems;
using Logiwall.IntegrationTests.Infrastructure;
using Logiwall.Shared.Resources;
using Logiwall.Shared.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Logiwall.IntegrationTests.Payments;

// Tests for plan 012 Phase A: the server is the only source of payment truth.
[Collection("api")]
public class PaymentFlowTests(SqlServerFixture fixture)
{
    private static object SelectBody(int orderId, int optionId, string callbackUrl = "https://example.test/cb") =>
        new { orderId, orderInqueryOptionId = optionId, channel = "pwa", callbackUrl };

    private static async Task<int> QuoteAsync(HttpClient client, int orderId)
    {
        var quote = await ApiClient.ReadJsonAsync(await client.GetAsync($"api/Order/GetOptions?orderId={orderId}"));
        return quote.GetProperty("data")[0].GetProperty("id").GetInt32();
    }

    private async Task<(HttpClient Client, int OrderId)> PendingPaymentAsync()
    {
        var (client, customerId) = await OrderTestData.RegisteredUserAsync(fixture);
        var orderId = await OrderTestData.InsertOrderAsync(fixture, customerId);
        var optionId = await QuoteAsync(client, orderId);
        (await client.PostAsJsonAsync("api/Order/SelectOption", SelectBody(orderId, optionId))).EnsureSuccessStatusCode();
        return (client, orderId);
    }

    private Task<List<(PaymentStatus Status, decimal Amount, Guid? Reference)>> PaymentsAsync(int orderId) =>
        fixture.WithDbAsync(async db => (await db.OrderPayments.Where(t => t.OrderId == orderId)
            .Select(t => new { t.Status, t.Amount, t.GatewayReferenceCode }).ToListAsync())
            .Select(t => (t.Status, t.Amount, t.GatewayReferenceCode)).ToList());

    [Fact]
    public async Task SelectOption_creates_pending_payment_with_server_amount()
    {
        var (_, orderId) = await PendingPaymentAsync();

        var payment = Assert.Single(await PaymentsAsync(orderId));
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(1100m, payment.Amount); // FakePostalProvider quotes FinalAmount = 1100
        Assert.NotNull(payment.Reference);
        Assert.Equal(1100m, fixture.Factory.Services.GetRequiredService<FakeSep724Provider>().LastAmount);
        var state = await fixture.WithDbAsync(db => db.Orders.Where(t => t.Id == orderId).Select(t => t.State).SingleAsync());
        Assert.Equal((short)OrderState.PaymentPending, state);
    }

    [Fact]
    public async Task Callback_url_outside_allow_list_is_rejected()
    {
        await using var factory = fixture.Factory.WithWebHostBuilder(b => b.UseSetting("Payment:AllowedCallbackOrigins:0", "https://app.test"));
        var gateway = factory.Services.GetRequiredService<FakeSep724Provider>();
        var (owner, customerId) = await OrderTestData.RegisteredUserAsync(fixture);
        var orderId = await OrderTestData.InsertOrderAsync(fixture, customerId);
        // Both hosts sign and validate with the same test certificate, so the owner's token works here too.
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = owner.DefaultRequestHeaders.Authorization;
        var optionId = await QuoteAsync(client, orderId);
        var callsBefore = gateway.TokenCalls;

        var response = await client.PostAsJsonAsync("api/Order/SelectOption", SelectBody(orderId, optionId, "https://evil.test/cb"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        PersianAssert.HasMessage(await ApiClient.ReadJsonAsync(response), nameof(AppStrings.InvalidCallbackUrl));
        Assert.Equal(callsBefore, gateway.TokenCalls);
        Assert.Empty(await PaymentsAsync(orderId));
    }

    [Fact]
    public async Task Verify_never_marks_success_in_phase_A()
    {
        var (client, orderId) = await PendingPaymentAsync();

        var response = await client.PostAsJsonAsync("api/Payment/VerifyPayment", new { orderId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ApiClient.ReadJsonAsync(response);
        PersianAssert.HasMessage(body, nameof(AppStrings.PaymentVerificationUnavailable));
        PersianAssert.HasMessage(body.GetProperty("data"), nameof(AppStrings.PaymentVerificationUnavailable));
        Assert.Equal(PaymentStatus.Pending, Assert.Single(await PaymentsAsync(orderId)).Status);
    }

    [Fact]
    public async Task Verify_finds_an_older_verified_payment()
    {
        var (client, orderId) = await PendingPaymentAsync();
        await fixture.WithDbAsync(async db =>
        {
            // The first link was paid and verified; then the customer opened a second link.
            var older = await db.OrderPayments.SingleAsync(t => t.OrderId == orderId);
            older.Status = PaymentStatus.Success;
            older.VerifiedAt = DateTimeOffset.Now;
            db.OrderPayments.Add(new OrderPayment
            {
                OrderId = orderId,
                Amount = older.Amount,
                PaymentType = older.PaymentType,
                Status = PaymentStatus.Pending,
                PayTrackingCode = "2",
                PayDate = DateTimeOffset.Now,
                GatewayRequestId = 2,
                GatewayReferenceCode = Guid.NewGuid(),
            });
            return await db.SaveChangesAsync();
        });

        var response = await client.PostAsJsonAsync("api/Payment/VerifyPayment", new { orderId });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal((int)PaymentStatus.Success, (await ApiClient.ReadJsonAsync(response)).GetProperty("data").GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Old_client_status_endpoint_is_gone()
    {
        var (client, orderId) = await PendingPaymentAsync();

        var response = await client.PostAsJsonAsync("api/Payment/UpdatePaymentOrderStatus", new { orderId });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Other_user_cannot_verify()
    {
        var (_, orderId) = await PendingPaymentAsync();
        var (other, _) = await OrderTestData.RegisteredUserAsync(fixture);

        var response = await other.PostAsJsonAsync("api/Payment/VerifyPayment", new { orderId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        PersianAssert.HasMessage(await ApiClient.ReadJsonAsync(response), nameof(AppStrings.InvalidOrder));
    }

    [Fact]
    public async Task Gateway_failure_returns_own_message_not_provider_text()
    {
        var gateway = fixture.Factory.Services.GetRequiredService<FakeSep724Provider>();
        var (client, customerId) = await OrderTestData.RegisteredUserAsync(fixture);
        var orderId = await OrderTestData.InsertOrderAsync(fixture, customerId);
        var optionId = await QuoteAsync(client, orderId);
        gateway.FailWithTitle = "Provider internal text";
        try
        {
            var response = await client.PostAsJsonAsync("api/Order/SelectOption", SelectBody(orderId, optionId));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            PersianAssert.HasMessage(await ApiClient.ReadJsonAsync(response), nameof(AppStrings.PaymentLinkFailed));
        }
        finally
        {
            gateway.FailWithTitle = null;
        }
    }
}
