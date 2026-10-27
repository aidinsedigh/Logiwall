using System.Net.Http.Json;
using System.Text.Json;
using Logiwall.Application.Abstractions.Providers;
using Logiwall.Shared.Dtos.Global;
using Logiwall.Shared.Dtos.Providers;
using Logiwall.Shared.Utils;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace Logiwall.Infrastructure.Providers;
public sealed class HttpProvider(IHttpClientFactory httpClientFactory, ILogger<HttpProvider> logger) : IHttpProvider
{
    private const int DefaultTimeoutSeconds = 30;

    public IEnumerable<(string Key, string? Value)>? DefaultHeaders { get; set; }

    public Task<TResponse?> DeleteAsync<TRequest, TResponse, TBody>(HttpProviderRequest<TBody, TRequest> request, Func<HttpResponseMessage, Task>? postCallHandler = null, Func<HttpResponseMessage, Task<TResponse?>>? decodeHandler = null, Func<TRequest?, TResponse?, Task<TResponse?>>? failHandler = null, CancellationToken cancellationToken = default)
        where TRequest : IHttpRequest
        where TResponse : IHttpResponse
        => SendAsync(HttpMethod.Delete, request, postCallHandler, decodeHandler, failHandler, cancellationToken);

    public Task<TResponse?> GetAsync<TRequest, TResponse, TBody>(HttpProviderRequest<TBody, TRequest> request, Func<HttpResponseMessage, Task>? postCallHandler = null, Func<HttpResponseMessage, Task<TResponse?>>? decodeHandler = null, Func<TRequest?, TResponse?, Task<TResponse?>>? failHandler = null, CancellationToken cancellationToken = default)
        where TRequest : IHttpRequest
        where TResponse : IHttpResponse
        => SendAsync(HttpMethod.Get, request, postCallHandler, decodeHandler, failHandler, cancellationToken);

    public Task<TResponse?> PostAsync<TRequest, TResponse, TBody>(HttpProviderRequest<TBody, TRequest> request, Func<HttpResponseMessage, Task>? postCallHandler = null, Func<HttpResponseMessage, Task<TResponse?>>? decodeHandler = null, Func<TRequest?, TResponse?, Task<TResponse?>>? failHandler = null, CancellationToken cancellationToken = default)
        where TRequest : IHttpRequest
        where TResponse : IHttpResponse
        => SendAsync(HttpMethod.Post, request, postCallHandler, decodeHandler, failHandler, cancellationToken);

    public Task<TResponse?> PutAsync<TRequest, TResponse, TBody>(HttpProviderRequest<TBody, TRequest> request, Func<HttpResponseMessage, Task>? postCallHandler = null, Func<HttpResponseMessage, Task<TResponse?>>? decodeHandler = null, Func<TRequest?, TResponse?, Task<TResponse?>>? failHandler = null, CancellationToken cancellationToken = default)
        where TRequest : IHttpRequest
        where TResponse : IHttpResponse
        => SendAsync(HttpMethod.Put, request, postCallHandler, decodeHandler, failHandler, cancellationToken);

    private async Task<TResponse?> SendAsync<TRequest, TResponse, TBody>(HttpMethod method, HttpProviderRequest<TBody, TRequest> request, Func<HttpResponseMessage, Task>? postCallHandler, Func<HttpResponseMessage, Task<TResponse?>>? decodeHandler, Func<TRequest?, TResponse?, Task<TResponse?>>? failHandler, CancellationToken cancellationToken)
        where TRequest : IHttpRequest
        where TResponse : IHttpResponse
    {
        var client = httpClientFactory.CreateClient();
        // HttpProviderRequest.Timeout is in seconds.
        client.Timeout = TimeSpan.FromSeconds(request.Timeout ?? DefaultTimeoutSeconds);

        if (method == HttpMethod.Get)
        {
            if (string.IsNullOrEmpty(request.BaseAddress) is false)
            {
                request.Uri = $"{request.BaseAddress.TrimEnd('/')}/{request.Uri}";
            }

            if (request.Body is not null)
            {
                request.Uri = QueryHelpers.AddQueryString(request.Uri!, Globals.ObjectToDictionary(request.Body)!.Select(t => new KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>(t.Key, t.Value?.ToString())));
            }
        }
        else if (string.IsNullOrEmpty(request.BaseAddress) is false)
        {
            client.BaseAddress = new Uri(request.BaseAddress);
        }

        if (DefaultHeaders is not null)
        {
            foreach (var header in DefaultHeaders)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        if (request.HeaderParameters?.Count > 0)
        {
            for (var i = 0; i < request.HeaderParameters.Count; i++)
            {
                client.DefaultRequestHeaders.Add(request.HeaderParameters[i].Key, request.HeaderParameters[i].Value);
            }
        }

        LogRequestDetails(method, request);

        using var message = new HttpRequestMessage(method, request.Uri) { Content = CreateContent(method, request.Body) };

        try
        {
            using var response = await client.SendAsync(message, cancellationToken);

            LogResponseDetails(response);

            if (postCallHandler is not null)
            {
                await postCallHandler(response);
            }

            TResponse? result;
            if (decodeHandler is not null)
            {
                result = await decodeHandler(response);
            }
            else
            {
                result = await DecodeJsonAsync<TResponse>(response, cancellationToken);
            }

            return response.StatusCode is not System.Net.HttpStatusCode.OK && failHandler is not null ? await failHandler(request.Request, result) : result;
        }
        catch (Exception exc) when (exc is not OperationCanceledException)
        {
            logger.LogError(exc, "[Http][Send][{Method}][{Uri}] Outbound request failed.", method, request.Uri);
            throw;
        }
    }

    private static HttpContent? CreateContent<TBody>(HttpMethod method, TBody? body) => body switch
    {
        _ when method == HttpMethod.Get => null,
        null when method == HttpMethod.Delete => null,
        List<KeyValuePair<string, string?>> keyValuePairBody => new FormUrlEncodedContent(keyValuePairBody),
        HttpContent httpContent => httpContent,
        _ => JsonContent.Create(body),
    };

    /// <summary>Reads a JSON body. An error page (HTML or broken JSON) gives <c>default</c>, not an exception.</summary>
    private async Task<TResponse?> DecodeJsonAsync<TResponse>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (mediaType?.Contains("json", StringComparison.OrdinalIgnoreCase) is not true)
        {
            logger.LogWarning("[Http][DecodeJson][{Uri}][{StatusCode}][{MediaType}] Response is not JSON.", response.RequestMessage?.RequestUri, (int)response.StatusCode, mediaType);
            return default;
        }

        try
        {
            return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "[Http][DecodeJson][{Uri}][{StatusCode}] Response JSON is not valid.", response.RequestMessage?.RequestUri, (int)response.StatusCode);
            return default;
        }
    }

    private void LogRequestDetails<TBody, TRequest>(HttpMethod method, HttpProviderRequest<TBody, TRequest> request)
    where TRequest : IHttpRequest
    {
        // Never log header values or bodies: they carry provider credentials and tokens.
        logger.LogInformation("[Http][Send][{Method}][{BaseAddress}][{Uri}][{HeaderNames}] Outbound request started.",
            method, request.BaseAddress, request.Uri,
            request.HeaderParameters is null ? string.Empty : string.Join(", ", request.HeaderParameters.Select(h => h.Key)));
    }

    private void LogResponseDetails(HttpResponseMessage response)
    {
        logger.LogInformation("[Http][Send][{Uri}][{StatusCode}] Outbound response received.",
            response.RequestMessage?.RequestUri, (int)response.StatusCode);
    }
}
