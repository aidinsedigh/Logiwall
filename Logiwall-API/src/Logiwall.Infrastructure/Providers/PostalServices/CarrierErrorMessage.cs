using System.Text.Json;

namespace Logiwall.Infrastructure.Providers.PostalServices;

internal static class CarrierErrorMessage
{
    public static string Parse(string raw, string fallback)
    {
        try
        {
            using var document = JsonDocument.Parse(raw);
            var messages = new List<string>();
            Visit(document.RootElement, messages, 0, document.RootElement.ValueKind == JsonValueKind.String);
            var message = string.Join("؛ ", messages.Distinct());
            return message.Length == 0 ? fallback : message[..Math.Min(2000, message.Length)];
        }
        catch (JsonException) { return fallback; }
    }

    private static void Visit(JsonElement value, List<string> messages, int depth, bool errorContent)
    {
        if (depth > 8 || messages.Count >= 20) return;
        if (value.ValueKind == JsonValueKind.String && errorContent)
        {
            var text = value.GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(text) && !text.Contains('<') && text.Length <= 2000)
                messages.Add(text);
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray()) Visit(item, messages, depth + 1, errorContent);
        }
        else if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in value.EnumerateObject())
            {
                var name = property.Name.ToLowerInvariant();
                if (name.Contains("token") || name.Contains("password") || name.Contains("apikey") || name is "stacktrace" or "exception") continue;
                if (name is "errors" or "validationerrors" or "error" or "message" or "messages" or "errormessage" or "description" or "error_description")
                    Visit(property.Value, messages, depth + 1, true);
                else if (name is "result" or "data" || errorContent && property.Value.ValueKind is JsonValueKind.Array or JsonValueKind.Object)
                    Visit(property.Value, messages, depth + 1, errorContent);
            }
            if (messages.Count == 0 && value.TryGetProperty("title", out var title)) Visit(title, messages, depth + 1, true);
        }
    }
}
