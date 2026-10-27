using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Logiwall.Application.Abstractions.Tools;

namespace Logiwall.IntegrationTests.Infrastructure;

/// <summary>Records the last SMS per destination instead of sending it.</summary>
public partial class FakeSmsService : ISMSService
{
    private readonly ConcurrentDictionary<string, string> lastMessages = new();

    public string SendSMS(string Message, string Destination)
    {
        lastMessages[Destination] = Message;
        return "OK";
    }

    public string LastCodeFor(string phone)
    {
        if (!lastMessages.TryGetValue(phone, out var message))
            throw new InvalidOperationException($"No SMS was sent to {phone}.");

        return OtpCodeRegex().Match(message).Value;
    }

    [GeneratedRegex(@"\d{5,6}")]
    private static partial Regex OtpCodeRegex();
}
