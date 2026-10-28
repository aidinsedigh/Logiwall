using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Logiwall.Application.Abstractions.Tools;

namespace Logiwall.IntegrationTests.Infrastructure;

/// <summary>Records the last SMS per destination instead of sending it.</summary>
public partial class FakeSmsService : ISMSService
{
    private readonly ConcurrentDictionary<string, string> lastMessages = new();
    private readonly ConcurrentDictionary<string, int> counts = new();

    /// <summary>The result the next sends return. "OK" means success.</summary>
    public string NextResult { get; set; } = "OK";

    public Task<string> SendSmsAsync(string message, string destination, CancellationToken cancellationToken = default)
    {
        lastMessages[destination] = message;
        counts.AddOrUpdate(destination, 1, (_, count) => count + 1);
        return Task.FromResult(NextResult);
    }

    public int CountFor(string phone) => counts.TryGetValue(phone, out var count) ? count : 0;

    public string LastCodeFor(string phone)
    {
        if (!lastMessages.TryGetValue(phone, out var message))
            throw new InvalidOperationException($"No SMS was sent to {phone}.");

        return OtpCodeRegex().Match(message).Value;
    }

    [GeneratedRegex(@"\d{5,6}")]
    private static partial Regex OtpCodeRegex();
}
