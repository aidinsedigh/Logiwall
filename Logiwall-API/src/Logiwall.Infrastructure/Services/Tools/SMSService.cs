using System.Text.Json;
using Logiwall.Application.Abstractions.Tools;
using Logiwall.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Logiwall.Infrastructure.Services.Tools;
public class SMSService(IHttpClientFactory httpClientFactory, IOptions<SmsOptions> smsOptions, ILogger<SMSService> logger) : ISMSService
{
    private readonly SmsOptions smsOptions = smsOptions.Value;

    private record PersiaFavaResult(
        [property: JsonPropertyName("result")] bool Result,
        [property: JsonPropertyName("error")] string? Error);

    public async Task<string> SendSmsAsync(string message, string destination, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient("sms");
            using var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("api_key", smsOptions.ApiKey),
                new KeyValuePair<string, string>("receiver_number", destination),
                new KeyValuePair<string, string>("note_arr[]", message),
                new KeyValuePair<string, string>("sender_number", smsOptions.SenderNumber),
            });

            using var response = await client.PostAsync(smsOptions.Url, content, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<PersiaFavaResult>(body);

            if (result?.Result is true)
                return "OK";

            return result?.Error ?? $"HTTP {(int)response.StatusCode}";
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            // Never log the message text: it contains the OTP.
            logger.LogWarning(ex, "[Sms][SendSms][{Destination}] SMS send failed.", MaskPhone(destination));
            return "ERROR";
        }
    }

    /// <summary>Keeps only the last 4 digits, so logs do not hold full phone numbers.</summary>
    public static string MaskPhone(string? phone)
        => string.IsNullOrEmpty(phone) || phone.Length <= 4 ? "****" : new string('*', phone.Length - 4) + phone[^4..];
}
