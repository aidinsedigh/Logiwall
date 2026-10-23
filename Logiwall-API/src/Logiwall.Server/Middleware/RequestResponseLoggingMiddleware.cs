using System.Text;
using System.Threading.Tasks;
using k8s.KubeConfigModels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace Logiwall.Server.Middleware;
// You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestResponseLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext httpContext)
    {

        // Log request
        await LogRequest(httpContext);

        // Log response
        await LogResponse(httpContext);
    }

    private async Task LogRequest(HttpContext context)
    {
        context.Request.EnableBuffering(); // Allows reading the request body multiple times

        var request = context.Request;
        var requestBody = await ReadStreamInChunks(request.Body);

        Log.Information("HTTP Request Information:{Environment.NewLine}" +
                        "Schema:{Schema} " +
                        "Host: {Host} " +
                        "Path: {Path} " +
                        "QueryString: {QueryString} " +
                        "Request Body: {RequestBody}",
            request.Scheme, request.Host, request.Path, request.QueryString, requestBody);

        // Reset the request body stream position so it can be read again by the next middleware
        request.Body.Position = 0;
    }

    private async Task LogResponse(HttpContext context)
    {
        var originalBodyStream = context.Response.Body;

        // Use a MemoryStream to capture the response body
        using (var responseBody = new MemoryStream())
        {
            // Replace the response body stream with our MemoryStream
            context.Response.Body = responseBody;

            // Call the next middleware in the pipeline
            await _next(context);

            // Log the response body
            var response = context.Response;
            response.Body.Seek(0, SeekOrigin.Begin); // Rewind the body to the start
            var responseBodyText = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin); // Reset position for writing back to client

            Log.Information("HTTP Response Information:{Environment.NewLine}" +
                            "Status Code: {StatusCode} " +
                            "Response Body: {ResponseBody}",
                response.StatusCode, responseBodyText);

            // Copy the contents of the MemoryStream back to the original response body stream
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }

    private async Task<string> ReadStreamInChunks(Stream stream)
    {
        const int bufferLength = 4096;
        stream.Seek(0, SeekOrigin.Begin);

        using (var reader = new StreamReader(stream, Encoding.UTF8, true, bufferLength, true))
        {
            return await reader.ReadToEndAsync();
        }
    }
}

// Extension method used to add the middleware to the HTTP request pipeline.
public static class RequestResponseLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestResponseLoggingMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestResponseLoggingMiddleware>();
    }
}
