using System.Net.Http.Json;
using Logiwall.Application.Abstractions.Providers;
using Logiwall.Shared.Dtos.Global;
using Logiwall.Shared.Dtos.Providers;
using Logiwall.Shared.Utils;
using Microsoft.AspNetCore.WebUtilities;
using Serilog;

namespace Logiwall.Infrastructure.Providers;
public sealed class HttpProvider(IHttpClientFactory httpClientFactory) : IHttpProvider
{
    public IEnumerable<(string Key, string? Value)>? DefaultHeaders { get; set; }

    public async Task<TResponse?> DeleteAsync<TRequest, TResponse, TBody>(HttpProviderRequest<TBody, TRequest> request, Func<HttpResponseMessage, Task>? postCallHandler = null, Func<HttpResponseMessage, Task<TResponse?>>? decodeHandler = null, Func<TRequest?, TResponse?, Task<TResponse?>>? failHandler = null)
        where TRequest : IHttpRequest
        where TResponse : IHttpResponse
    {
        var client = httpClientFactory.CreateClient();
        if (request.Timeout.HasValue)
        {
            client.Timeout = TimeSpan.FromMilliseconds(request.Timeout.Value);
        }

        if (string.IsNullOrEmpty(request.BaseAddress) is false)
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

        HttpResponseMessage response;

        if (request.Body is null)
        {
            response = await client.DeleteAsync(request.Uri);
        }
        else
        {
            using var req = new HttpRequestMessage(HttpMethod.Delete, request.Uri);
            if (request.Body is List<KeyValuePair<string, string?>> keyValuePairBody)
            {
                req.Content = new FormUrlEncodedContent(keyValuePairBody);
            }
            else
            {
                req.Content = JsonContent.Create(request.Body);
            }

            response = await client.SendAsync(req);
        }

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
            result = await response.Content.ReadFromJsonAsync<TResponse>();
        }

        return response.StatusCode is not System.Net.HttpStatusCode.OK && failHandler is not null ? await failHandler(request.Request, result) : result;
    }

    public async Task<TResponse?> GetAsync<TRequest, TResponse, TBody>(HttpProviderRequest<TBody, TRequest> request, Func<HttpResponseMessage, Task>? postCallHandler = null, Func<HttpResponseMessage, Task<TResponse?>>? decodeHandler = null, Func<TRequest?, TResponse?, Task<TResponse?>>? failHandler = null)
        where TRequest : IHttpRequest
        where TResponse : IHttpResponse
    {
        var client = httpClientFactory.CreateClient();
        if (request.Timeout.HasValue)
        {
            client.Timeout = TimeSpan.FromMilliseconds(request.Timeout.Value);
        }

        if (string.IsNullOrEmpty(request.BaseAddress) is false)
        {
            request.Uri = $"{request.BaseAddress.TrimEnd('/')}/{request.Uri}";
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
            for (int i = 0; i < request.HeaderParameters.Count; i++)
            {
                client.DefaultRequestHeaders.Add(request.HeaderParameters[i].Key, request.HeaderParameters[i].Value);
            }
        }

        if (request.Body is not null)
        {
            request.Uri = QueryHelpers.AddQueryString(request.Uri!, Globals.ObjectToDictionary(request.Body)!.Select(t => new KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>(t.Key, t.Value?.ToString())));
        }

        var response = await client.GetAsync(request.Uri);

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
            result = await response.Content.ReadFromJsonAsync<TResponse>();
        }

        return response.StatusCode is not System.Net.HttpStatusCode.OK && failHandler is not null ? await failHandler(request.Request, result) : result;

    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse, TBody>(HttpProviderRequest<TBody, TRequest> request, Func<HttpResponseMessage, Task>? postCallHandler = null, Func<HttpResponseMessage, Task<TResponse?>>? decodeHandler = null, Func<TRequest?, TResponse?, Task<TResponse?>>? failHandler = null)
        where TRequest : IHttpRequest
        where TResponse : IHttpResponse
    {
        var client = httpClientFactory.CreateClient();

        Log.Information("Starting POST request to {Uri} with base address {BaseAddress}", request.Uri, request.BaseAddress);

        if (request.Timeout.HasValue)
        {
            client.Timeout = TimeSpan.FromMilliseconds(request.Timeout.Value);
        }

        if (string.IsNullOrEmpty(request.BaseAddress) is false)
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

        LogRequestDetails(request);

        HttpResponseMessage response;
        try
        {
            if (request.Body is List<KeyValuePair<string, string?>> keyValuePairBody)
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, request.Uri) { Content = new FormUrlEncodedContent(keyValuePairBody), };
                response = await client.SendAsync(req);
            }
            else if (request.Body is HttpContent httpContent)
            {
                response = await client.PostAsync(request.Uri, httpContent);
            }
            else
            {
                response = await client.PostAsJsonAsync(request.Uri, request.Body);
            }

            await LogResponseDetails(response);

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
                result = await response.Content.ReadFromJsonAsync<TResponse>();
            }

            return response.StatusCode is not System.Net.HttpStatusCode.OK && failHandler is not null ? await failHandler(request.Request, result) : result;
        }
        catch (Exception exc)
        {
            Log.Error(exc, "An error occurred while making a POST request to {Uri}", request.Uri);

            throw;
        }
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse, TBody>(HttpProviderRequest<TBody, TRequest> request, Func<HttpResponseMessage, Task>? postCallHandler = null, Func<HttpResponseMessage, Task<TResponse?>>? decodeHandler = null, Func<TRequest?, TResponse?, Task<TResponse?>>? failHandler = null)
        where TRequest : IHttpRequest
        where TResponse : IHttpResponse
    {
        var client = httpClientFactory.CreateClient();
        if (request.Timeout.HasValue)
        {
            client.Timeout = TimeSpan.FromMilliseconds(request.Timeout.Value);
        }

        if (string.IsNullOrEmpty(request.BaseAddress) is false)
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

        HttpResponseMessage response;

        if (request.Body is List<KeyValuePair<string, string?>> keyValuePairBody)
        {
            using var req = new HttpRequestMessage(HttpMethod.Put, request.Uri) { Content = new FormUrlEncodedContent(keyValuePairBody), };
            response = await client.SendAsync(req);
        }
        else if (request.Body is HttpContent httpContent)
        {
            response = await client.PutAsync(request.Uri, httpContent);
        }
        else
        {
            response = await client.PutAsJsonAsync(request.Uri, request.Body);
        }

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
            result = await response.Content.ReadFromJsonAsync<TResponse>();
        }

        return response.StatusCode is not System.Net.HttpStatusCode.OK && failHandler is not null ? await failHandler(request.Request, result) : result;

    }
    private void LogRequestDetails<TBody, TRequest>(HttpProviderRequest<TBody, TRequest> request)
    where TRequest : IHttpRequest
    {
        // Never log header values or bodies: they carry provider credentials and tokens.
        Log.Information("Outbound request to {BaseAddress}{Uri} with header names: {HeaderNames}",
            request.BaseAddress, request.Uri,
            request.HeaderParameters is null ? string.Empty : string.Join(", ", request.HeaderParameters.Select(h => h.Key)));
    }

    private Task LogResponseDetails(HttpResponseMessage response)
    {
        Log.Information("Outbound response from {Uri}: {StatusCode}",
            response.RequestMessage?.RequestUri, (int)response.StatusCode);
        return Task.CompletedTask;
    }
}
