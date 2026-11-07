using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Logiwall.Application.Abstractions.Providers;
using Logiwall.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Logiwall.Infrastructure.Providers.PostalServices;

public class ETipaxCarrier(IHttpClientFactory clients, IOptions<ETipaxOptions> settings,
    IStringLocalizer<AppStrings> localizer,
    ProviderTokenCache tokens) : IShippingCarrier
{
    public string Code => "etipax";
    private const string TokenKey = "etipax";
    // shortcut: the token reply may omit its lifetime; 10 minutes is then assumed. A 401 drops the cached token.
    private static readonly TimeSpan DefaultTokenLifetime = TimeSpan.FromMinutes(10);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static int PaymentCode(int method) => (Constants.PaymentMethod)method switch
    {
        Constants.PaymentMethod.Online => 10,
        Constants.PaymentMethod.CashOnDelivery => 20,
        Constants.PaymentMethod.RecipientPayOnDelivery => 30,
        Constants.PaymentMethod.SenderCreditOnDelivery => 40,
        Constants.PaymentMethod.SenderWallet => 50,
        Constants.PaymentMethod.SenderWalletOnDelivery => 60,
        Constants.PaymentMethod.SenderCash => 80,
        Constants.PaymentMethod.AgencyCredit => 90,
        _ => throw new BadRequestException("ShippingPaymentMethodMismatch")
    };

    public bool MatchesPaymentMethod(string requestJson, Constants.PaymentMethod paymentMethod)
    {
        try
        {
            using var doc = JsonDocument.Parse(requestJson);
            return doc.RootElement.TryGetProperty("packages", out var packages) && packages.ValueKind == JsonValueKind.Array
                && packages.GetArrayLength() > 0 && packages.EnumerateArray().All(p => p.ValueKind == JsonValueKind.Object
                    && p.TryGetProperty("paymentType", out var mode) && mode.ValueKind == JsonValueKind.Number
                    && mode.TryGetInt32(out var value) && value == PaymentCode((int)paymentMethod));
        }
        catch (JsonException) { return false; }
    }

    private async Task<string> GetTokenAsync(CancellationToken ct) =>
        await tokens.GetOrCreateAsync(TokenKey, () => RequestTokenAsync(ct))
            ?? throw new JsonException("ETipax did not return an access token.");

    private void ForgetTokenOnUnauthorized(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized) tokens.Invalidate(TokenKey);
    }

    private async Task<(string? Token, TimeSpan Lifetime)> RequestTokenAsync(CancellationToken ct)
    {
        var o = settings.Value;
        if (string.IsNullOrWhiteSpace(o.Username) || string.IsNullOrWhiteSpace(o.Password) || string.IsNullOrWhiteSpace(o.ApiKey))
            throw new InvalidOperationException("Providers:ETipax credentials are not configured.");
        using var client = clients.CreateClient("etipax");
        using var response = await client.PostAsJsonAsync("/api/OM/v3/Account/token",
            new { o.Username, o.Password, o.ApiKey }, ct);
        var raw = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new CarrierRequestException(CarrierErrorMessage.Parse(raw, localizer["ShippingCarrierValidationFailed"]), CarrierStatus((int)response.StatusCode));
        using var body = JsonDocument.Parse(raw);
        var seconds = String(body.RootElement, "expiresIn", "expires_in");
        return (String(body.RootElement, "accessToken", "access_token", "token"),
            int.TryParse(seconds, NumberStyles.Integer, CultureInfo.InvariantCulture, out var s) && s > 0
                ? TimeSpan.FromSeconds(s) : DefaultTokenLifetime);
    }

    public string SerializeRegistration(CarrierShipmentRequest request) => JsonSerializer.Serialize(new
    {
        packages = request.Parcels.Select(p => new
        {
            origin = Address(p.Origin), destination = Address(p.Destination),
            weight = p.WeightKilograms, packageValue = p.ValueRials, p.Length, p.Width, p.Height,
            packageContentId = int.Parse(p.ContentCode, CultureInfo.InvariantCulture),
            packingId = int.Parse(p.PackingCode, CultureInfo.InvariantCulture), p.PackType,
            request.ServiceId, paymentType = PaymentCode(request.PaymentType), pickupType = 10, distributionType = 10,
            cod = p.CodRials, cashAmount = 0m
        }),
        request.TraceCode, secondaryTraceCode = (string?)null, discountCode = (string?)null,
        successCallbackUrl = "", failureCallbackUrl = ""
    }, Json);

    private static object Address(CarrierAddress a) => new
    {
        cityId = int.Parse(a.CityCode, CultureInfo.InvariantCulture), fullAddress = a.FullAddress,
        latitude = a.Latitude is null ? null : (decimal?)decimal.Round(a.Latitude.Value, 6),
        longitude = a.Longitude is null ? null : (decimal?)decimal.Round(a.Longitude.Value, 6),
        floor = (string?)null, a.Unit, a.PostalCode, description = (string?)null,
        beneficiary = new { a.FullName, a.Mobile }
    };

    public async Task<IReadOnlyList<CarrierQuote>> QuoteAsync(CarrierShipmentRequest request, CancellationToken cancellationToken)
    {
        if (request.ServiceId <= 0)
            throw new BadRequestException("ShippingServiceCatalogMissing");
        _ = PaymentCode(request.PaymentType);
        var token = await GetTokenAsync(cancellationToken);
        var selected = request;
        var payload = new
        {
            packageInputs = selected.Parcels.Select(p => new
            {
                origin = new { cityId = int.Parse(p.Origin.CityCode, CultureInfo.InvariantCulture) },
                destination = new { cityId = int.Parse(p.Destination.CityCode, CultureInfo.InvariantCulture) },
                weight = p.WeightKilograms, packageValue = p.ValueRials, p.Length, p.Width, p.Height,
                packingId = int.Parse(p.PackingCode, CultureInfo.InvariantCulture),
                packageContentId = int.Parse(p.ContentCode, CultureInfo.InvariantCulture), p.PackType,
                paymentType = PaymentCode(selected.PaymentType), pickupType = 10, distributionType = 10, selected.ServiceId,
                parcelBookId = 0
            }), discountCode = (string?)null
        };
        using var client = clients.CreateClient("etipax");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.PostAsJsonAsync("/api/OM/v3/Pricing", payload, cancellationToken);
        ForgetTokenOnUnauthorized(response);
        if (!response.IsSuccessStatusCode)
        {
            var message = CarrierErrorMessage.Parse(await response.Content.ReadAsStringAsync(cancellationToken),
                localizer["ShippingCarrierValidationFailed"]);
            // shortcut: eTipax sends no error code; its "سگمنت" (segment) text means the service does not accept the pack type. Match a code when eTipax adds one.
            if (message.Contains("سگمنت", StringComparison.Ordinal)) message = localizer["ShippingServiceParcelTypeUnsupported"];
            throw new CarrierRequestException(message, CarrierStatus((int)response.StatusCode));
        }
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (Boolean(document.RootElement, "isSuccess", "success", "succeeded") is false)
            throw new CarrierRequestException(CarrierErrorMessage.Parse(document.RootElement.GetRawText(), localizer["ShippingCarrierValidationFailed"]), 400);
        var amount = CalculateQuote(document.RootElement, selected.Parcels.Count, selected.ServiceId);
        return amount is null ? [] : [new CarrierQuote(selected.ServiceId, amount.Value, SerializeRegistration(selected))];
    }

    public static decimal? CalculateQuote(JsonElement response, int parcelCount, int serviceId)
    {
        if (parcelCount <= 0 || response.ValueKind != JsonValueKind.Array || response.GetArrayLength() != parcelCount) return null;
        decimal total = 0;
        foreach (var parcel in response.EnumerateArray())
        {
            if (parcel.ValueKind != JsonValueKind.Object) return null;
            var rates = parcel.EnumerateObject().Where(p => p.Value.ValueKind == JsonValueKind.Object
                && p.Value.TryGetProperty("serviceId", out var id) && id.ValueKind == JsonValueKind.Number && id.TryGetInt32(out var code) && code == serviceId).ToArray();
            if (rates.Length != 1 || !rates[0].Value.TryGetProperty("finalPrice", out var price)
                || price.ValueKind != JsonValueKind.Number || !price.TryGetDecimal(out var amount) || amount < 0) return null;
            total += amount;
        }
        return total;
    }

    public Task<CarrierRegistrationResult> RegisterAsync(string requestJson, CancellationToken cancellationToken)
        => SendMutationAsync("/api/om/v3/Orders", requestJson, cancellationToken);

    public Task<CarrierRegistrationResult> CancelAsync(string externalOrderId, CancellationToken cancellationToken)
        => SendMutationAsync("/api/OM/v3/Orders/CancelOrder/" + Uri.EscapeDataString(externalOrderId), null, cancellationToken);

    private async Task<CarrierRegistrationResult> SendMutationAsync(string path, string? requestJson, CancellationToken ct)
    {
        // Any login failure happens before the shipping request is sent, so it is a definite rejection the customer may retry.
        string token;
        try { token = await GetTokenAsync(ct); }
        catch (CarrierRequestException error)
        {
            return new(false, false, (int)error.StatusCode, null, [], null, "CarrierAuthenticationRejected", ErrorMessage: error.Message);
        }
        catch (Exception e) when (e is HttpRequestException or OperationCanceledException or JsonException or InvalidOperationException)
        {
            return new(false, false, null, null, [], null, "CarrierAuthenticationUnavailable");
        }
        try
        {
            using var client = clients.CreateClient("etipax");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var content = requestJson is null ? null : new StringContent(requestJson, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(path, content, ct);
            ForgetTokenOnUnauthorized(response);
            var raw = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                return new(false, (int)response.StatusCode >= 500 || (int)response.StatusCode is 408 or 429,
                    (int)response.StatusCode, null, [], raw, "CarrierRejected",
                    ErrorMessage: CarrierErrorMessage.Parse(raw, localizer["ShippingCarrierValidationFailed"]));
            try
            {
                using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(raw) ? "{}" : raw);
                if (Boolean(doc.RootElement, "isSuccess", "success", "succeeded") is false)
                    return new(false, false, 400, null, [], raw, "CarrierRejected",
                        ErrorMessage: CarrierErrorMessage.Parse(raw, localizer["ShippingCarrierValidationFailed"]));
                var orderId = String(doc.RootElement, "orderId", "tipaxOrderId", "id");
                var codes = Strings(doc.RootElement, "trackingCodes", "tipaxTrackingCodes", "barcodes", "parcelCodes");
                if (codes.Count == 0 && String(doc.RootElement, "trackingCode", "tipaxTrackingCode", "barcode", "parcelCode") is { } code)
                    codes.Add(code);
                var isRegistration = requestJson is not null;
                return new(!isRegistration || orderId is not null, isRegistration && orderId is null,
                    (int)response.StatusCode, orderId, codes, raw, orderId is null && isRegistration ? "IncompleteCarrierResponse" : null,
                    CarrierStatusCode: isRegistration ? "59" : "57");
            }
            catch (JsonException) { return new(false, true, (int)response.StatusCode, null, [], raw, "InvalidCarrierResponse"); }
        }
        catch (Exception e) when (e is HttpRequestException or OperationCanceledException)
        { return new(false, true, null, null, [], null, "TransportOutcomeUnknown"); }
    }

    public async Task<IReadOnlyList<CarrierReference>> GetReferencesAsync(CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync(cancellationToken);
        var references = new List<CarrierReference>();
        foreach (var (category, path) in new[] { ("packaging", "PackingPrices"), ("content", "PackContentRates"), ("status", "ParcelStatus") })
        {
            using var client = clients.CreateClient("etipax");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await client.GetAsync("/api/OM/v3/" + path, cancellationToken);
            ForgetTokenOnUnauthorized(response);
            response.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = doc.RootElement;
            if (Boolean(root, "isSuccess") is false) throw new JsonException("ETipax rejected reference synchronization.");
            var items = root.ValueKind == JsonValueKind.Array ? root
                : root.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.Array ? result
                : root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array ? data
                : throw new JsonException("ETipax reference array is missing.");
            if (items.GetArrayLength() == 0) throw new JsonException("An empty catalogue cannot deactivate all references.");
            foreach (var item in items.EnumerateArray())
                references.Add(new(category, String(item, "id") ?? throw new JsonException("Reference id is missing."),
                    String(item, "title") ?? throw new JsonException("Reference title is missing."), item.GetRawText()));
        }
        return references;
    }

    // Carrier credentials are server credentials; their rejection must not log out the customer.
    internal static int CarrierStatus(int status) => status is 401 or 403 ? 502 : status;

    private static IEnumerable<JsonElement> Objects(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object) yield break;
        yield return element;
        foreach (var p in element.EnumerateObject().Where(p => p.Name is "result" or "data"))
            foreach (var child in Objects(p.Value)) yield return child;
    }
    private static string? String(JsonElement e, params string[] names) => Objects(e)
        .SelectMany(o => o.EnumerateObject()).Where(p => names.Contains(p.Name)
            && p.Value.ValueKind is JsonValueKind.String or JsonValueKind.Number)
        .Select(p => p.Value.ToString()).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
    private static bool? Boolean(JsonElement e, params string[] names) => Objects(e)
        .SelectMany(o => o.EnumerateObject()).Where(p => names.Contains(p.Name)
            && p.Value.ValueKind is JsonValueKind.True or JsonValueKind.False)
        .Select(p => (bool?)p.Value.GetBoolean()).FirstOrDefault();
    private static List<string> Strings(JsonElement e, params string[] names) => Objects(e)
        .SelectMany(o => o.EnumerateObject()).Where(p => names.Contains(p.Name) && p.Value.ValueKind == JsonValueKind.Array)
        .SelectMany(p => p.Value.EnumerateArray()).Where(v => v.ValueKind is JsonValueKind.String or JsonValueKind.Number)
        .Select(v => v.ToString()).Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
}

