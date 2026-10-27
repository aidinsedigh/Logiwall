using System.Text.Json;
using System.Text.RegularExpressions;

namespace Logiwall.IntegrationTests.Infrastructure;

/// <summary>ADR-0003: a client message is Persian when it is not empty and has no ASCII letter.</summary>
public static class PersianAssert
{
    public static void IsPersian(string? message)
    {
        Assert.False(string.IsNullOrWhiteSpace(message), "The message is empty.");
        Assert.False(Regex.IsMatch(message, "[A-Za-z]"), $"The message is not Persian: {message}");
    }

    /// <summary>Asserts the <c>message</c> property of a response body.</summary>
    public static void HasPersianMessage(JsonElement body) => IsPersian(body.GetProperty("message").GetString());
}
