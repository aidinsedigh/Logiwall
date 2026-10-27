using System.Net;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Net.Http.Headers;

namespace Logiwall.Server.Services;

public partial class ServerExceptionHandler : IExceptionHandler
{
    [AutoInject] private IStringLocalizer<AppStrings> localizer = default!;
    [AutoInject] private JsonSerializerOptions jsonSerializerOptions = default!;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception e, CancellationToken cancellationToken)
    {
        // Using the Request-Id header, one can find the log for server-related exceptions.
        // UseExceptionHandler cleared the headers, so X-Correlation-Id is set again (ADR-0004).
        httpContext.Response.Headers.Append(HeaderNames.RequestId, httpContext.TraceIdentifier);
        httpContext.Response.Headers[CorrelationIdMiddleware.HeaderName] = httpContext.TraceIdentifier;

        var exception = UnWrapException(e);
        var knownException = exception as KnownException;

        // Only known exceptions return their own message; an unknown one never shows its details, Development too (ADR-0003).
        var key = knownException?.Key ?? nameof(UnknownException);
        var message = knownException?.Message ?? localizer[nameof(UnknownException)];

        var statusCode = (int)(exception is RestException restExp ? restExp.StatusCode : HttpStatusCode.InternalServerError);

        if (exception is KnownException && message == key)
        {
            message = localizer[message];
        }

        var restExceptionPayload = new RestErrorInfo
        {
            Key = key,
            Message = message,
            ExceptionType = knownException?.GetType().FullName ?? typeof(UnknownException).FullName
        };

        if (exception is ResourceValidationException validationException)
        {
            restExceptionPayload.Payload = validationException.Payload;
        }

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(restExceptionPayload, jsonSerializerOptions.GetTypeInfo<RestErrorInfo>(), cancellationToken: cancellationToken);

        return true;
    }

    private Exception UnWrapException(Exception exp)
    {
        return exp is TargetInvocationException && exp.InnerException is not null
            ? exp.InnerException
            : exp;
    }
}
