using System.Net;
using System.Text.Json;
using Logiwall.Application.Abstractions.Providers;
using Logiwall.Infrastructure.Options;
using Logiwall.Infrastructure.Providers.PostalServices;
using Logiwall.Shared.Dtos.OrderItems;
using Microsoft.Extensions.Options;

namespace Logiwall.UnitTests.Providers;

public class ETipaxCarrierTests
{
    [Theory]
    [InlineData("504", 20)]
    [InlineData("521", 60)]
    public async Task No_packaging_uses_the_same_carrier_reference_for_pricing_and_registration(string code, int packType)
    {
        var carrier = Create(async (http, ct) =>
        {
            if (http.RequestUri!.AbsolutePath.EndsWith("token"))
                return new(HttpStatusCode.OK) { Content = new StringContent("{\"token\":\"test\"}") };
            using var body = JsonDocument.Parse(await http.Content!.ReadAsStringAsync(ct));
            var package = body.RootElement.GetProperty("packageInputs")[0];
            Assert.Equal(int.Parse(code), package.GetProperty("packingId").GetInt32());
            Assert.Equal(packType, package.GetProperty("packType").GetInt32());
            Assert.Equal(42, package.GetProperty("packageContentId").GetInt32());
            return new(HttpStatusCode.OK) { Content = new StringContent("[{\"regularRate\":{\"serviceId\":4,\"finalPrice\":150000}}]") };
        });
        var address = new CarrierAddress("1", "address", 35, 51, "1", null, "name", "09123456789");
        var request = new CarrierShipmentRequest("test", [new(1, address, address, 2, 1000, 10, 10, 10, code, "42", packType, 0)], 4, 72, 0, 0);
        var quote = Assert.Single(await carrier.QuoteAsync(request, CancellationToken.None));
        using var json = JsonDocument.Parse(quote.RequestJson);
        var parcel = json.RootElement.GetProperty("packages")[0];
        Assert.Equal(int.Parse(code), parcel.GetProperty("packingId").GetInt32());
        Assert.Equal(packType, parcel.GetProperty("packType").GetInt32());
        Assert.Equal(42, parcel.GetProperty("packageContentId").GetInt32());
    }

    [Theory]
    [InlineData(71, 10)]
    [InlineData(72, 20)]
    [InlineData(73, 30)]
    [InlineData(74, 40)]
    [InlineData(75, 50)]
    [InlineData(76, 60)]
    [InlineData(77, 80)]
    [InlineData(78, 90)]
    public void Shared_payment_methods_are_mapped_only_in_the_adapter(int method, int expectedCode)
    {
        var carrier = Create((_, _) => throw new InvalidOperationException());
        var address = new CarrierAddress("1", "address", 35, 51, "1", null, "name", "09123456789");
        var request = new CarrierShipmentRequest("test", [new(1, address, address, 2, 1000, 10, 10, 10, "1", "1", 1, 0)], 4, method, 0, 0);
        var raw = carrier.SerializeRegistration(request);
        using var json = JsonDocument.Parse(raw);
        Assert.Equal(expectedCode, json.RootElement.GetProperty("packages")[0].GetProperty("paymentType").GetInt32());
        Assert.True(carrier.MatchesPaymentMethod(raw, (Logiwall.Shared.Enums.Constants.PaymentMethod)method));
        Assert.False(carrier.MatchesPaymentMethod(raw, method == 71
            ? Logiwall.Shared.Enums.Constants.PaymentMethod.CashOnDelivery : Logiwall.Shared.Enums.Constants.PaymentMethod.Online));
    }

    [Fact]
    public async Task Only_the_selected_service_is_quoted_without_environment_modes()
    {
        var tokenCalls = 0;
        var pricingCalls = 0;
        var carrier = Create(async (http, ct) =>
        {
            if (http.RequestUri!.AbsolutePath.EndsWith("token"))
            {
                Interlocked.Increment(ref tokenCalls);
                return new(HttpStatusCode.OK) { Content = new StringContent("{\"token\":\"test\"}") };
            }
            using var json = JsonDocument.Parse(await http.Content!.ReadAsStringAsync(ct));
            Interlocked.Increment(ref pricingCalls);
            var parcel = json.RootElement.GetProperty("packageInputs")[0];
            Assert.Equal(20, parcel.GetProperty("paymentType").GetInt32());
            Assert.Equal(10, parcel.GetProperty("pickupType").GetInt32());
            return parcel.GetProperty("serviceId").GetInt32() == 1
                ? new(HttpStatusCode.BadRequest) { Content = new StringContent("{}") }
                : new(HttpStatusCode.OK) { Content = new StringContent("[{\"regularRate\":{\"serviceId\":4,\"finalPrice\":150000}}]") };
        });
        var address = new CarrierAddress("1", "address", 35, 51, "1", null, "name", "09123456789");
        var request = new CarrierShipmentRequest("test", [new(1, address, address, 2, 1000, 10, 10, 10, "1", "1", 1, 0)], 4, 72, 0, 0);
        var quotes = await carrier.QuoteAsync(request, CancellationToken.None);
        Assert.Single(quotes);
        Assert.Equal(4, quotes[0].ServiceId);
        Assert.Equal(150000, quotes[0].AmountRials);
        Assert.Equal(1, tokenCalls);
        Assert.Equal(1, pricingCalls);
    }

    [Fact]
    public void Quote_sums_only_selected_service_for_every_parcel()
    {
        using var json = JsonDocument.Parse("""[{"regularRate":{"serviceId":1,"finalPrice":1200},"expressRate":{"serviceId":2,"finalPrice":9000}},{"regularRate":{"serviceId":1,"finalPrice":3400}}]""");
        Assert.Equal(4600m, ETipaxCarrier.CalculateQuote(json.RootElement, 2, 1));
        Assert.Null(ETipaxCarrier.CalculateQuote(json.RootElement, 2, 2));
        Assert.Null(ETipaxCarrier.CalculateQuote(json.RootElement, 1, 1));
    }

    [Theory]
    [InlineData("[null]")]
    [InlineData("[{\"rate\":{\"serviceId\":\"1\",\"finalPrice\":1}}]")]
    [InlineData("[{\"rate\":{\"serviceId\":1,\"finalPrice\":null}}]")]
    [InlineData("[{\"rate\":{\"serviceId\":1,\"finalPrice\":-1}}]")]
    [InlineData("[{\"a\":{\"serviceId\":1,\"finalPrice\":1},\"b\":{\"serviceId\":1,\"finalPrice\":2}}]")]
    public void Malformed_or_ambiguous_rates_never_become_quotes(string response)
    {
        using var json = JsonDocument.Parse(response);
        Assert.Null(ETipaxCarrier.CalculateQuote(json.RootElement, 1, 1));
    }

    [Fact]
    public void Registration_preserves_kilograms_rials_and_provider_reference_codes()
    {
        var carrier = Create((_, _) => throw new InvalidOperationException());
        var address = new CarrierAddress("1899", "address", 36.12345678m, 52.12345678m, "2", "1234567890", "name", "09123456789");
        var request = new CarrierShipmentRequest("logiwall-123", [new(4, address, address, 2.5m, 4000000m, 10, 20, 30, "42", "7", 1, 0)], 1, 71, 0, 0);
        using var json = JsonDocument.Parse(carrier.SerializeRegistration(request));
        var parcel = json.RootElement.GetProperty("packages")[0];
        Assert.Equal(2.5m, parcel.GetProperty("weight").GetDecimal());
        Assert.Equal(4000000m, parcel.GetProperty("packageValue").GetDecimal());
        Assert.Equal(42, parcel.GetProperty("packingId").GetInt32());
        Assert.Equal(7, parcel.GetProperty("packageContentId").GetInt32());
        Assert.Equal(10, parcel.GetProperty("paymentType").GetInt32());
        Assert.Equal(10, parcel.GetProperty("pickupType").GetInt32());
        Assert.Equal(10, parcel.GetProperty("distributionType").GetInt32());
        Assert.Equal(1899, parcel.GetProperty("origin").GetProperty("cityId").GetInt32());
        Assert.Equal(36.123457m, parcel.GetProperty("origin").GetProperty("latitude").GetDecimal());
    }

    [Theory]
    [InlineData(200, "{\"result\":{\"id\":123,\"barcodes\":[\"a\",\"b\"]}}", true, false)]
    [InlineData(200, "{}", false, true)]
    [InlineData(200, "broken", false, true)]
    [InlineData(500, "error", false, true)]
    [InlineData(408, "error", false, true)]
    [InlineData(400, "error", false, false)]
    [InlineData(422, "{\"errors\":{\"packages[0].weight\":[\"internal diagnostic\"]}}", false, false)]
    public async Task Mutation_classifies_uncertain_outcomes_without_retry(int status, string body, bool success, bool ambiguous)
    {
        var mutations = 0;
        var carrier = Create((request, _) =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("token"))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"accessToken\":\"test-token\"}") });
            mutations++;
            Assert.Equal("/api/om/v3/Orders", request.RequestUri.AbsolutePath);
            Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
            return Task.FromResult(new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent(body) });
        });
        var result = await carrier.RegisterAsync("{\"packages\":[]}", CancellationToken.None);
        Assert.Equal(success, result.Succeeded);
        Assert.Equal(ambiguous, result.IsAmbiguous);
        Assert.Equal(1, mutations);
        if (status >= 400) Assert.NotNull(result.ErrorMessage);
        else Assert.Null(result.ValidationErrors);
        if (success) { Assert.Equal("123", result.ExternalOrderId); Assert.Equal(new[] { "a", "b" }, result.TrackingCodes); }
    }

    // Trimmed from a real jet-test reply (2026-11-07) for barcode ba800197458ir.
    private const string TrackingReply = """
        {"IsSuccess":true,"Message":"با موفقیت انجام شد","Data":[{"ContractKindName":"اکسپرس","ContractStatusName":"ابطال شده",
        "ContractCreateDate":"1405/08/16 12:36:15","FinalAmount":10395953.65,"PayInPlaceAmount":10395953.65,"COD":0.00,
        "PaymentKindName":"پس کرایه","SenderFullName":"محمودی عرفان","SenderMobileNumber":"09100000000","SenderCityName":"تهران",
        "SenderProvinceName":"تهران","ReceiverFullName":"عرفان محمودی","ReceiverCityName":"شیراز","ReceiverProvinceName":"فارس",
        "Barcode":"ba800197458ir","states":[
          {"StateNo":12,"StateTitle":"ابطال بسته","OperationDateTime":"1405/08/16 12:38:10","OperationNodeName":"فروش","OperationCityName":"تهران","Reason":"درخواست مشتری"},
          {"StateNo":1,"StateTitle":"ثبت","OperationDateTime":"1405/08/16 12:36:15","OperationNodeName":"فروش","OperationCityName":"تهران","Reason":""}]}]}
        """;

    [Fact]
    public async Task Tracking_maps_the_jet_reply_into_the_shared_contract()
    {
        var carrier = Create(async (http, ct) =>
        {
            Assert.Equal("/odata/Tipax/DspContractTrackings/Tipax.TrackingParcelWithDetail", http.RequestUri!.AbsolutePath);
            Assert.Equal("jet-token", http.Headers.Authorization!.Parameter);
            Assert.Equal("{\"Code\":\"ba800197458ir\"}", await http.Content!.ReadAsStringAsync(ct));
            return new(HttpStatusCode.OK) { Content = new StringContent(TrackingReply) };
        }, new ETipaxOptions { TrackingToken = "jet-token" });

        var tracking = await carrier.TrackAsync("ba800197458ir", CancellationToken.None);

        var iran = TimeSpan.FromHours(3.5);
        Assert.NotNull(tracking);
        Assert.Equal("ابطال شده", tracking.Status);
        Assert.Equal("اکسپرس", tracking.ServiceName);
        Assert.Equal(new DateTimeOffset(2026, 11, 7, 12, 36, 15, iran), tracking.RegisteredAt);
        Assert.Equal(new ParcelTrackingPartyDto("محمودی عرفان", "تهران", "تهران"), tracking.Sender);
        Assert.Equal(new ParcelTrackingPartyDto("عرفان محمودی", "شیراز", "فارس"), tracking.Receiver);
        // The carrier list is out of order on purpose: the contract sorts the journey oldest first.
        Assert.Equal(new[] { "ثبت", "ابطال بسته" }, tracking.Journey.Select(e => e.Title));
        Assert.Equal(new ParcelTrackingEventDto("ابطال بسته", new DateTimeOffset(2026, 11, 7, 12, 38, 10, iran), "تهران", "فروش", "درخواست مشتری"),
            tracking.Journey[1]);
        Assert.Equal(new ParcelTrackingPaymentDto("پس کرایه", 10395953.65m, 10395953.65m, 0m), tracking.Payment);
    }

    [Fact]
    public async Task Tracking_unknown_code_returns_null()
    {
        var carrier = Create((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent("{\"IsSuccess\":false,\"Message\":\" کد رهگیری وارد شده اشتباه است\",\"Data\":[]}") }),
            new ETipaxOptions { TrackingToken = "jet-token" });

        Assert.Null(await carrier.TrackAsync("ZZ999999999IR", CancellationToken.None));
    }

    [Fact]
    public async Task Tracking_fails_without_a_token_or_on_a_rejected_token()
    {
        var noToken = Create((_, _) => throw new InvalidOperationException("No HTTP call is expected."));
        await Assert.ThrowsAsync<InvalidOperationException>(() => noToken.TrackAsync("ba800197458ir", CancellationToken.None));

        var rejected = Create((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)),
            new ETipaxOptions { TrackingToken = "expired" });
        await Assert.ThrowsAsync<HttpRequestException>(() => rejected.TrackAsync("ba800197458ir", CancellationToken.None));
    }

    [Theory]
    [InlineData("1405/08/16 12:38:10", "2026-11-07T12:38:10+03:30")]
    [InlineData("1403/12/30 00:00:00", "2025-03-20T00:00:00+03:30")]
    [InlineData("1405/13/01 00:00:00", null)]
    [InlineData("2026-11-07T12:38:10", null)]
    [InlineData(null, null)]
    public void Jalali_times_are_read_as_iran_time(string? text, string? expected)
        => Assert.Equal(expected is null ? null : DateTimeOffset.Parse(expected, System.Globalization.CultureInfo.InvariantCulture),
            ETipaxCarrier.JalaliTime(text));

    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => respond(request, ct);
    }
    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, false) { BaseAddress = new Uri("https://carrier.test") };
    }
    private static ETipaxCarrier Create(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond, ETipaxOptions? options = null)
        => new(new Factory(new Handler(respond)), Microsoft.Extensions.Options.Options.Create(options ?? new ETipaxOptions
        { Username = "test", Password = "test", ApiKey = "test" }),
            new Microsoft.Extensions.Localization.StringLocalizer<Logiwall.Shared.Resources.AppStrings>(
                new Microsoft.Extensions.Localization.ResourceManagerStringLocalizerFactory(
                    Microsoft.Extensions.Options.Options.Create(new Microsoft.Extensions.Localization.LocalizationOptions()),
                    Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance)),
            new Logiwall.Infrastructure.Providers.ProviderTokenCache(
                new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions())));

    [Theory]
    [InlineData("network")]
    [InlineData("no-token")]
    [InlineData("not-configured")]
    public async Task Login_failure_before_sending_is_a_definite_rejection(string failure)
    {
        var mutations = 0;
        var carrier = Create((request, _) =>
        {
            if (!request.RequestUri!.AbsolutePath.EndsWith("token"))
            {
                mutations++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
            return failure == "network"
                ? throw new HttpRequestException("DNS failure")
                : Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
        }, failure == "not-configured" ? new ETipaxOptions() : null);

        var result = await carrier.RegisterAsync("{\"packages\":[]}", CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.False(result.IsAmbiguous);
        Assert.Equal(0, mutations);
    }

    [Fact]
    public async Task Token_is_reused_until_the_carrier_rejects_it()
    {
        var tokenCalls = 0;
        var rejectNext = false;
        var carrier = Create((request, _) =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("token"))
            {
                tokenCalls++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"accessToken\":\"t\",\"expiresIn\":3600}") });
            }
            if (rejectNext)
            {
                rejectNext = false;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("{}") });
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"orderId\":1}") });
        });

        await carrier.RegisterAsync("{}", CancellationToken.None);
        await carrier.RegisterAsync("{}", CancellationToken.None);
        Assert.Equal(1, tokenCalls);

        rejectNext = true;
        await carrier.RegisterAsync("{}", CancellationToken.None); // 401 drops the cached token
        await carrier.RegisterAsync("{}", CancellationToken.None);
        Assert.Equal(2, tokenCalls);
    }

    [Theory]
    [InlineData("{\"errors\":{\"packageInputs[0].weight\":[\"وزن نامعتبر است\"],\"packageInputs[0].packingId\":[\"بسته‌بندی نامعتبر است\"]}}", "وزن نامعتبر است؛ بسته‌بندی نامعتبر است")]
    [InlineData("{\"result\":{\"message\":\"اعتبار کافی نیست\",\"accessToken\":\"secret-token\"}}", "اعتبار کافی نیست")]
    [InlineData("\"حداکثر ارتفاع بسته ۷ سانتیمتر است\"", "حداکثر ارتفاع بسته ۷ سانتیمتر است")]
    public async Task Pricing_validation_returns_the_carrier_message_and_status(string body, string message)
    {
        var carrier = Create((request, _) => Task.FromResult(new HttpResponseMessage(
            request.RequestUri!.AbsolutePath.EndsWith("token") ? HttpStatusCode.OK : HttpStatusCode.UnprocessableEntity)
        { Content = new StringContent(request.RequestUri.AbsolutePath.EndsWith("token")
            ? "{\"accessToken\":\"test-token\"}"
            : body) }));
        var address = new CarrierAddress("1", "address", 35, 51, "1", null, "name", "09123456789");
        var request = new CarrierShipmentRequest("test", [new(1, address, address, 2, 1000, 10, 10, 10, "1", "1", 1, 0)], 1, 71, 0, 0);
        var error = await Assert.ThrowsAsync<Logiwall.Shared.Exceptions.CarrierRequestException>(() => carrier.QuoteAsync(request, CancellationToken.None));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, error.StatusCode);
        Assert.Equal(message, error.Message);
    }

    // Bodies are the eTipax test API replies (2026-11-07): a bare JSON string or a message object.
    [Theory]
    [InlineData("{\"message\":\"نوع سگمنت برای یک یا تعدادی از مرسوله ها یافت نشد.\",\"status\":400}", "ShippingServiceParcelTypeUnsupported")]
    [InlineData("\"نوع سگمنت برای یک یا تعدادی از مرسوله ها یافت نشد.\"", "ShippingServiceParcelTypeUnsupported")]
    [InlineData("\"ارزش مرسوله با بارکد  در بازه ی تعریف شده برای سگمنت بسته نمیگنجد.\"", "ShippingParcelValueTooHigh")]
    [InlineData("\"ارتفاع مرسوله با بارکد  بیش از مقدار مجاز است.\"", "ShippingParcelDimensionTooLarge")]
    public async Task Pricing_segment_rejection_becomes_a_clear_message(string body, string key)
    {
        var carrier = Create((request, _) => Task.FromResult(request.RequestUri!.AbsolutePath.EndsWith("token")
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"accessToken\":\"test-token\"}") }
            : new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(body) }));
        var address = new CarrierAddress("1", "address", 35, 51, "1", null, "name", "09123456789");
        var request = new CarrierShipmentRequest("test", [new(1, address, address, 2, 1000, 10, 10, 10, "521", "30", 60, 0)], 12, 72, 0, 0);

        var error = await Assert.ThrowsAsync<Logiwall.Shared.Exceptions.CarrierRequestException>(() => carrier.QuoteAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.BadRequest, error.StatusCode);
        Assert.Equal(Logiwall.Shared.Resources.AppStrings.ResourceManager.GetString(key), error.Message);
    }
}

