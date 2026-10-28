using Serilog.Context;

namespace Logiwall.Server.Services;

/// <summary>
/// Gives each request one correlation ID (ADR-0004): in <see cref="HttpContext.TraceIdentifier"/>,
/// in the <c>X-Correlation-Id</c> response header and in the <c>CorrelationId</c> log property.
/// </summary>
public class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var id = Sanitise(context.Request.Headers[HeaderName]);
        context.TraceIdentifier = id;
        context.Response.Headers[HeaderName] = id;

        using (LogContext.PushProperty("CorrelationId", id))
        {
            await next(context);
        }
    }

    /// <summary>
    /// Keeps a client value of 1-64 ASCII letters, digits, '-' or '_'; else makes a GUID.
    /// The value goes into a response header and the log, so it must not carry injected text.
    /// </summary>
    public static string Sanitise(string? value) =>
        value is { Length: >= 1 and <= 64 } && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_')
            ? value
            : Guid.NewGuid().ToString();
}
