using System.Collections.Concurrent;
using Logiwall.Application.Abstractions.Providers;

namespace Logiwall.IntegrationTests.Infrastructure;

public class FakeShippingCarrier : IShippingCarrier
{
    public string Code => "etipax";
    public ConcurrentDictionary<string, int> Calls { get; } = new();
    public ConcurrentBag<CarrierShipmentRequest> QuoteRequests { get; } = [];
    public ConcurrentDictionary<string, ConcurrentQueue<CarrierRegistrationResult>> Results { get; } = new();
    public ConcurrentDictionary<string, (TaskCompletionSource Started, TaskCompletionSource Continue)> QuoteGates { get; } = new();
    private readonly ConcurrentDictionary<string, Logiwall.Shared.Enums.Constants.PaymentMethod> quotedMethods = new();
    /// <summary>While set, quotes fail the way an unreachable carrier does.</summary>
    public volatile bool FailQuotes;
    /// <summary>Carrier service codes the fake rejects the way eTipax rejects an unsupported pack type.</summary>
    public ConcurrentDictionary<int, byte> RejectedServices { get; } = new();
    /// <summary>The price the fake quotes for a trace code; 1000 rials when absent.</summary>
    public ConcurrentDictionary<string, decimal> QuoteAmounts { get; } = new();
    public async Task<IReadOnlyList<CarrierQuote>> QuoteAsync(CarrierShipmentRequest request, CancellationToken ct)
    {
        if (FailQuotes) throw new HttpRequestException("Fake carrier is down.");
        QuoteRequests.Add(request);
        if (RejectedServices.ContainsKey(request.ServiceId))
            throw new Logiwall.Shared.Exceptions.CarrierRequestException("Service does not accept this pack type.", 400);
        if (QuoteGates.TryGetValue(request.TraceCode, out var gate))
        {
            gate.Started.TrySetResult();
            await gate.Continue.Task.WaitAsync(ct);
        }
        quotedMethods[request.TraceCode] = (Logiwall.Shared.Enums.Constants.PaymentMethod)request.PaymentType;
        return [new(request.ServiceId, QuoteAmounts.GetValueOrDefault(request.TraceCode, 1000), SerializeRegistration(request))];
    }
    public string SerializeRegistration(CarrierShipmentRequest request) => request.TraceCode;
    public bool MatchesPaymentMethod(string requestJson, Logiwall.Shared.Enums.Constants.PaymentMethod paymentMethod)
    {
        if (!requestJson.StartsWith("{")) return quotedMethods.TryGetValue(requestJson, out var method)
            ? method == paymentMethod : paymentMethod == Logiwall.Shared.Enums.Constants.PaymentMethod.Online;
        using var doc = System.Text.Json.JsonDocument.Parse(requestJson);
        var expected = (int)paymentMethod switch { 71 => 10, 72 => 20, 73 => 30, 74 => 40, 75 => 50, 76 => 60, 77 => 80, 78 => 90, _ => -1 };
        return doc.RootElement.GetProperty("packages")[0].GetProperty("paymentType").GetInt32() == expected;
    }
    public Task<CarrierRegistrationResult> RegisterAsync(string requestJson, CancellationToken ct)
    {
        Calls.AddOrUpdate(requestJson, 1, (_, n) => n + 1);
        if (Results.TryGetValue(requestJson, out var results) && results.TryDequeue(out var result))
            return Task.FromResult(result);
        return Task.FromResult(requestJson.StartsWith("unknown")
            ? new CarrierRegistrationResult(false, true, null, null, [], null, "TransportOutcomeUnknown")
            : new CarrierRegistrationResult(true, false, 200, "external-" + requestJson, ["barcode-" + requestJson], "{}", null));
    }
    public Task<CarrierRegistrationResult> CancelAsync(string id, CancellationToken ct)
        => Task.FromResult(new CarrierRegistrationResult(true, false, 200, id, [], "{}", null));
    public Task<IReadOnlyList<CarrierReference>> GetReferencesAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<CarrierReference>>([
            new("packaging", "1", "Test packaging", "{\"id\":1,\"packType\":1}"),
            new("content", "1", "Test content", "{\"id\":1}"),
            new("status", "1", "Test status", "{\"id\":1}")]);
}
