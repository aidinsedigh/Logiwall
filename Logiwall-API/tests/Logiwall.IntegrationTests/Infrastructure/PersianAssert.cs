using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Logiwall.Shared.Resources;

namespace Logiwall.IntegrationTests.Infrastructure;

/// <summary>ADR-0003: a client message is Persian when it is not empty and has no ASCII letter.</summary>
public static class PersianAssert
{
    private static readonly CultureInfo Persian = new("fa-IR");

    /// <summary>The Persian text of an <see cref="AppStrings"/> key.</summary>
    public static string Text(string key) => AppStrings.ResourceManager.GetString(key, Persian)!;

    public static void IsPersian(string? message)
    {
        Assert.False(string.IsNullOrWhiteSpace(message), "The message is empty.");
        Assert.False(Regex.IsMatch(message, "[A-Za-z]"), $"The message is not Persian: {message}");
    }

    /// <summary>Asserts the <c>message</c> property of a response body.</summary>
    public static void HasPersianMessage(JsonElement body) => IsPersian(body.GetProperty("message").GetString());

    /// <summary>Asserts that the <c>message</c> property is the Persian text of <paramref name="key"/>.</summary>
    public static void HasMessage(JsonElement body, string key)
    {
        HasPersianMessage(body);
        Assert.Equal(Text(key), body.GetProperty("message").GetString());
    }

    /// <summary>Asserts each <c>message</c> property in a body, the <c>RestErrorInfo.Payload</c> errors too.</summary>
    public static void AllMessagesArePersian(JsonElement body)
    {
        var messages = Messages(body).ToList();
        Assert.NotEmpty(messages);
        Assert.All(messages, IsPersian);
    }

    private static IEnumerable<string?> Messages(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Array => element.EnumerateArray().SelectMany(Messages),
        JsonValueKind.Object => element.EnumerateObject().SelectMany(p =>
            p.NameEquals("message") && p.Value.ValueKind == JsonValueKind.String ? [p.Value.GetString()] : Messages(p.Value)),
        _ => []
    };

    /// <summary>Every string value in a body, for shapes without a <c>message</c> property (ModelState).</summary>
    public static IEnumerable<string> Strings(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => [element.GetString()!],
        JsonValueKind.Array => element.EnumerateArray().SelectMany(Strings),
        JsonValueKind.Object => element.EnumerateObject().SelectMany(p => Strings(p.Value)),
        _ => []
    };
}
