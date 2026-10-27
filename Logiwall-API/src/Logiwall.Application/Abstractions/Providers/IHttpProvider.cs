using Logiwall.Shared.Dtos.Global;
using Logiwall.Shared.Dtos.Providers;

namespace Logiwall.Application.Abstractions.Providers;
public interface IHttpProvider
{
    Task<TResponse?> PostAsync<TRequest, TResponse, TBody>(HttpProviderRequest<TBody, TRequest> request, Func<HttpResponseMessage, Task>? postCallHandler = null, Func<HttpResponseMessage, Task<TResponse?>>? decodeHandler = null, Func<TRequest?, TResponse?, Task<TResponse?>>? failHandler = null, CancellationToken cancellationToken = default)
        where TRequest : IHttpRequest
        where TResponse : IHttpResponse;

    Task<TResponse?> PutAsync<TRequest, TResponse, TBody>(HttpProviderRequest<TBody, TRequest> request, Func<HttpResponseMessage, Task>? postCallHandler = null, Func<HttpResponseMessage, Task<TResponse?>>? decodeHandler = null, Func<TRequest?, TResponse?, Task<TResponse?>>? failHandler = null, CancellationToken cancellationToken = default)
        where TRequest : IHttpRequest
        where TResponse : IHttpResponse;

    Task<TResponse?> GetAsync<TRequest, TResponse, TBody>(HttpProviderRequest<TBody, TRequest> request, Func<HttpResponseMessage, Task>? postCallHandler = null, Func<HttpResponseMessage, Task<TResponse?>>? decodeHandler = null, Func<TRequest?, TResponse?, Task<TResponse?>>? failHandler = null, CancellationToken cancellationToken = default)
        where TRequest : IHttpRequest
        where TResponse : IHttpResponse;

    Task<TResponse?> DeleteAsync<TRequest, TResponse, TBody>(HttpProviderRequest<TBody, TRequest> request, Func<HttpResponseMessage, Task>? postCallHandler = null, Func<HttpResponseMessage, Task<TResponse?>>? decodeHandler = null, Func<TRequest?, TResponse?, Task<TResponse?>>? failHandler = null, CancellationToken cancellationToken = default)
        where TRequest : IHttpRequest
        where TResponse : IHttpResponse;
}
