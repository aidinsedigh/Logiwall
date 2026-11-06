using System.Net.Http.Json;
using Logiwall.Domain.Entities.OrderItems;
using Logiwall.IntegrationTests.Infrastructure;
using Logiwall.Shared.Dtos.OrderItems;
using Logiwall.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Logiwall.Shared.Enums.Constants;

namespace Logiwall.IntegrationTests.Orders;

[Collection("api")]
public class NoPackagingTests(SqlServerFixture fixture) : IAsyncLifetime
{
    private readonly LogiwallApiFactory factory = new(fixture.ConnectionString, useLegacyPostalProvider: false);
    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => factory.DisposeAsync().AsTask();

    private async Task<(HttpClient Client, UpdateOrderRequestDto Form, int ServiceId, int NoneId)> ArrangeAsync(int packType)
    {
        var api = new ApiClient(factory, factory.Services.GetRequiredService<FakeSmsService>());
        var client = api.Authed(await api.LoginWithOtpAsync(ApiClient.NewPhone()));
        await ApiClient.RegisterAsync(client, "Strong-Pass-1");
        var form = new UpdateOrderRequestDto
        {
            ChannelId = 2, SenderName = "Sender", SenderLastName = "Test", SenderPhoneNumber = "09123456789",
            OriginAddress = "Origin", OriginLat = "35.7", OriginLng = "51.4", OriginalPlaque = "1", OriginalUnit = "2",
            ReceiverName = "Receiver", ReceiverLastName = "Test", ReceiverPhoneNumber = "09123456780",
            DestinationAddress = "Destination", DestinationLat = "37.2", DestinationLng = "49.5", DestinationPlaque = "3", DestinationUnit = "4"
        };
        var ids = await fixture.WithDbAsync(async db =>
        {
            var provider = await db.ServiceProviders.SingleAsync(p => p.Code == "etipax");
            var code = packType == 60 ? "521" : "504";
            var none = await db.ProviderReferenceItems.SingleOrDefaultAsync(r => r.ServiceProviderId == provider.Id
                && r.Category == "packaging" && r.ExternalCode == code);
            if (none is null)
            {
                none = new() { ServiceProviderId = provider.Id, Category = "packaging", ExternalCode = code };
                db.ProviderReferenceItems.Add(none);
            }
            none.Title = "No carrier packaging";
            none.MetadataJson = $"{{\"packType\":{packType}}}";
            none.IsActive = true;
            var content = new ProviderReferenceItem { ServiceProviderId = provider.Id, Category = "content",
                ExternalCode = Guid.NewGuid().ToString("N"), Title = "No packaging test content",
                MetadataJson = $"{{\"packType\":{packType}}}", IsActive = true };
            db.ProviderReferenceItems.Add(content);
            form.OriginCity = (short)await db.StaticDataMappers.Where(m => m.ProviderType == ProviderType.ETipax
                && m.StaticData!.Type == StaticDataType.OriginCity).Select(m => m.StaticDataId).FirstAsync();
            form.DestinationCity = (short)await db.StaticDataMappers.Where(m => m.ProviderType == ProviderType.ETipax
                && m.StaticData!.Type == StaticDataType.DestinationCity).Select(m => m.StaticDataId).FirstAsync();
            await db.SaveChangesAsync();
            return (ContentId: content.Id, NoneId: none.Id,
                ServiceId: await db.ProviderReferenceItems.Where(r => r.ServiceProviderId == provider.Id
                    && r.Category == "service" && r.IsActive).Select(r => r.Id).FirstAsync());
        });
        form.OrderParcels = [new() { ParcelTypeId = ParcelType.Package, PackageTypeId = 0, PackagingReferenceId = null,
            ContentReferenceId = ids.ContentId, Weight = packType == 60 ? 35 : 2, ParcelValue = 100000,
            Length = 20, Width = 20, Height = 20 }];
        var created = await client.PostAsJsonAsync("api/Order/CreateOrder", form);
        created.EnsureSuccessStatusCode();
        form.OrderId = (await ApiClient.ReadJsonAsync(created)).GetProperty("data").GetProperty("orderId").GetInt32();
        return (client, form, ids.ServiceId, ids.NoneId);
    }

    [Theory]
    [InlineData(20, "504")]
    [InlineData(60, "521")]
    public async Task No_packaging_can_be_created_edited_quoted_and_registered(int type, string expectedCode)
    {
        var (client, form, serviceId, _) = await ArrangeAsync(type);
        form.OriginAddress = "Updated origin";
        (await client.PostAsJsonAsync("api/Order/UpdateOrder", form)).EnsureSuccessStatusCode();
        var quoteResponse = await client.GetAsync($"api/Order/GetOptions?orderId={form.OrderId}&serviceReferenceId={serviceId}&paymentType=72");
        quoteResponse.EnsureSuccessStatusCode();
        var quoteId = (await ApiClient.ReadJsonAsync(quoteResponse)).GetProperty("data")[0].GetProperty("orderOptions")[0].GetProperty("id").GetInt32();
        var trace = await fixture.WithDbAsync(db => db.OrderInqueryOptions.Where(q => q.Id == quoteId).Select(q => q.CarrierRequestJson).SingleAsync());
        var carrier = factory.Services.GetRequiredService<FakeShippingCarrier>();
        var request = carrier.QuoteRequests.Single(q => q.TraceCode == trace);
        Assert.Equal(expectedCode, request.Parcels[0].PackingCode);
        Assert.Equal(type, request.Parcels[0].PackType);
        (await OrderTestData.SelectAsync(client, form.OrderId, quoteId)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("api/Shipment/Register", new { form.OrderId })).EnsureSuccessStatusCode();
        Assert.Equal(1, carrier.Calls[trace!]);
    }

    [Fact]
    public async Task Content_filters_use_the_selected_packaging_or_active_no_packaging_types()
    {
        var (client, form, _, noneId) = await ArrangeAsync(20);
        var flyerId = await fixture.WithDbAsync(async db =>
        {
            var providerId = await db.ProviderReferenceItems.Where(r => r.Id == noneId).Select(r => r.ServiceProviderId).SingleAsync();
            var flyer = new ProviderReferenceItem { ServiceProviderId = providerId, Category = "content", ExternalCode = "filter-flyer",
                Title = "Flyer test content", IsActive = true, MetadataJson = "{\"packType\":50}" };
            db.ProviderReferenceItems.Add(flyer);
            await db.SaveChangesAsync(); return flyer.Id;
        });
        foreach (var filter in new[] { "packageTypeId=0", $"packageTypeId=0&packagingReferenceId={noneId}", $"packagingReferenceId={noneId}" })
        {
            var response = await client.GetAsync("api/Shipment/References?providerCode=tipax&category=content&" + filter);
            response.EnsureSuccessStatusCode();
            var rows = (await ApiClient.ReadJsonAsync(response)).GetProperty("data").EnumerateArray().ToArray();
            Assert.Contains(rows, r => r.GetProperty("id").GetInt32() == form.OrderParcels[0].ContentReferenceId);
            Assert.DoesNotContain(rows, r => r.GetProperty("id").GetInt32() == flyerId);
            Assert.All(rows, r => Assert.Contains(r.GetProperty("packType").GetInt32(), new[] { 20, 60 }));
        }
        var all = (await ApiClient.ReadJsonAsync(await client.GetAsync("api/Shipment/References?providerCode=tipax&category=content"))).GetProperty("data");
        Assert.Contains(all.EnumerateArray(), r => r.GetProperty("id").GetInt32() == flyerId);
        var conflicting = await client.GetAsync($"api/Shipment/References?category=content&packageTypeId=1&packagingReferenceId={noneId}");
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, conflicting.StatusCode);
    }

    [Fact]
    public async Task Packaging_filters_use_content_and_keep_no_packaging_choices_separate()
    {
        var (client, form, _, noneId) = await ArrangeAsync(20);
        foreach (var type in new[] { 0, 1 })
        {
            var response = await client.GetAsync($"api/Shipment/References?category=packaging&contentReferenceId={form.OrderParcels[0].ContentReferenceId}&packageTypeId={type}");
            response.EnsureSuccessStatusCode();
            var rows = (await ApiClient.ReadJsonAsync(response)).GetProperty("data").EnumerateArray().ToArray();
            Assert.All(rows, r =>
            {
                Assert.Equal(20, r.GetProperty("packType").GetInt32());
                Assert.Equal(type == 1, r.GetProperty("requiresPackaging").GetBoolean());
            });
            if (type == 0) Assert.Contains(rows, r => r.GetProperty("id").GetInt32() == noneId);
            else Assert.DoesNotContain(rows, r => r.GetProperty("id").GetInt32() == noneId);
        }
    }

    [Theory]
    [InlineData("category=content&packageTypeId=2")]
    [InlineData("category=content&packagingReferenceId=0")]
    [InlineData("category=content&contentReferenceId=1")]
    [InlineData("category=packaging&packagingReferenceId=1")]
    [InlineData("category=status&packageTypeId=0")]
    public async Task Invalid_reference_filters_are_rejected(string query)
    {
        var (client, _, _, _) = await ArrangeAsync(20);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, (await client.GetAsync("api/Shipment/References?" + query)).StatusCode);
    }

    [Fact]
    public async Task Reference_filters_reject_inactive_and_foreign_provider_ids()
    {
        var (client, _, _, noneId) = await ArrangeAsync(20);
        var foreignId = await fixture.WithDbAsync(async db =>
        {
            var other = new Logiwall.Domain.Entities.OrderItems.ServiceProvider { Name = "Filter test carrier", Title = "Other", Code = Guid.NewGuid().ToString("N"), IsActive = true };
            var reference = new ProviderReferenceItem { ServiceProvider = other, Category = "packaging", ExternalCode = "504",
                Title = "Other provider packaging", IsActive = true, MetadataJson = "{\"packType\":20}" };
            db.ProviderReferenceItems.Add(reference);
            await db.ProviderReferenceItems.Where(r => r.Id == noneId).ExecuteUpdateAsync(s => s.SetProperty(r => r.IsActive, false));
            await db.SaveChangesAsync(); return reference.Id;
        });
        foreach (var id in new[] { foreignId, noneId })
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, (await client.GetAsync($"api/Shipment/References?category=content&packagingReferenceId={id}")).StatusCode);
    }

    [Fact]
    public async Task One_order_can_mix_packaging_choices_and_reject_a_stale_selection_after_toggle()
    {
        var (client, form, serviceId, noneId) = await ArrangeAsync(20);
        var regularId = await fixture.WithDbAsync(async db =>
        {
            var providerId = await db.ProviderReferenceItems.Where(r => r.Id == noneId).Select(r => r.ServiceProviderId).SingleAsync();
            var packing = new ProviderReferenceItem { ServiceProviderId = providerId, Category = "packaging",
                ExternalCode = "987654321", Title = "Regular packaging test", IsActive = true, MetadataJson = "{\"packType\":20}" };
            db.ProviderReferenceItems.Add(packing);
            await db.SaveChangesAsync(); return packing.Id;
        });
        form.OrderParcels.Add(form.OrderParcels[0] with { PackageTypeId = 1, PackagingReferenceId = regularId });
        (await client.PostAsJsonAsync("api/Order/UpdateOrder", form)).EnsureSuccessStatusCode();
        var response = await client.GetAsync($"api/Order/GetOptions?orderId={form.OrderId}&serviceReferenceId={serviceId}&paymentType=72");
        response.EnsureSuccessStatusCode();
        var parcels = Assert.Single(factory.Services.GetRequiredService<FakeShippingCarrier>().QuoteRequests).Parcels;
        Assert.Equal(new[] { "504", "987654321" }, parcels.Select(p => p.PackingCode));
        var catalog = (await ApiClient.ReadJsonAsync(await client.GetAsync("api/Shipment/References?providerCode=tipax&category=packaging"))).GetProperty("data");
        Assert.True(catalog.EnumerateArray().Single(r => r.GetProperty("id").GetInt32() == regularId).GetProperty("requiresPackaging").GetBoolean());
        form.OrderParcels[1].PackageTypeId = 0;
        (await client.PostAsJsonAsync("api/Order/UpdateOrder", form)).EnsureSuccessStatusCode();
        var invalid = await client.GetAsync($"api/Order/GetOptions?orderId={form.OrderId}&serviceReferenceId={serviceId}&paymentType=72");
        Assert.Equal("ShippingPackagingIncompatible", (await ApiClient.ReadJsonAsync(invalid)).GetProperty("key").GetString());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Missing_or_inactive_no_packaging_reference_is_rejected_before_carrier_call(bool inactive)
    {
        var (client, form, serviceId, noneId) = await ArrangeAsync(20);
        await fixture.WithDbAsync(async db =>
        {
            var none = await db.ProviderReferenceItems.SingleAsync(r => r.Id == noneId);
            if (inactive) none.IsActive = false;
            else none.MetadataJson = "{\"packType\":70}";
            await db.SaveChangesAsync(); return true;
        });
        var response = await client.GetAsync($"api/Order/GetOptions?orderId={form.OrderId}&serviceReferenceId={serviceId}&paymentType=72");
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("ShippingNoPackagingUnavailable", (await ApiClient.ReadJsonAsync(response)).GetProperty("key").GetString());
        var trace = await fixture.WithDbAsync(db => db.Orders.Where(o => o.Id == form.OrderId).Select(o => "logiwall-" + o.OrderNumber).SingleAsync());
        Assert.DoesNotContain(factory.Services.GetRequiredService<FakeShippingCarrier>().QuoteRequests, q => q.TraceCode == trace);
    }

    [Fact]
    public async Task Packaging_catalog_identifies_no_packaging_records_and_explicit_selection_works()
    {
        var (client, form, serviceId, noneId) = await ArrangeAsync(20);
        var catalog = (await ApiClient.ReadJsonAsync(await client.GetAsync("api/Shipment/References?providerCode=tipax&category=packaging"))).GetProperty("data");
        Assert.False(catalog.EnumerateArray().Single(r => r.GetProperty("id").GetInt32() == noneId).GetProperty("requiresPackaging").GetBoolean());
        form.OrderParcels[0].PackagingReferenceId = noneId;
        (await client.PostAsJsonAsync("api/Order/UpdateOrder", form)).EnsureSuccessStatusCode();
        (await client.GetAsync($"api/Order/GetOptions?orderId={form.OrderId}&serviceReferenceId={serviceId}&paymentType=72")).EnsureSuccessStatusCode();
        form.OrderParcels[0].PackageTypeId = 1;
        (await client.PostAsJsonAsync("api/Order/UpdateOrder", form)).EnsureSuccessStatusCode();
        var invalid = await client.GetAsync($"api/Order/GetOptions?orderId={form.OrderId}&serviceReferenceId={serviceId}&paymentType=72");
        Assert.Equal("ShippingPackagingIncompatible", (await ApiClient.ReadJsonAsync(invalid)).GetProperty("key").GetString());
    }
}
