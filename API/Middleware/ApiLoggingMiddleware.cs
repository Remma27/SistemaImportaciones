using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Sistema_de_Gestion_de_Importaciones.Middleware
{
    public class ApiLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiLoggingMiddleware> _logger;

        public ApiLoggingMiddleware(RequestDelegate next, ILogger<ApiLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Path.StartsWithSegments("/api"))
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var requestTime = DateTime.UtcNow;

            var requestMethod = context.Request.Method;
            var requestPath = context.Request.Path;
            var requestQuery = context.Request.QueryString.ToString();
            var requestContentType = context.Request.ContentType;
            var userIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = context.Request.Headers["User-Agent"].ToString();

            var requestId = Guid.NewGuid().ToString();
            context.Items["RequestId"] = requestId;

            _logger.LogInformation(
                "API Request {RequestId}: {Method} {Path}{Query} | " +
                "ContentType: {ContentType} | IP: {IP} | UserAgent: {UserAgent}",
                requestId, requestMethod, requestPath, requestQuery,
                requestContentType, userIp, userAgent);

            var originalResponseBody = context.Response.Body;
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            try
            {
                await _next(context);

                stopwatch.Stop();

                context.Response.Body.Seek(0, SeekOrigin.Begin);

                await responseBodyStream.CopyToAsync(originalResponseBody);

                _logger.LogInformation(
                    "API Response {RequestId}: StatusCode {StatusCode} | " +
                    "Duration: {Duration}ms | ContentType: {ContentType}",
                    requestId, context.Response.StatusCode, stopwatch.ElapsedMilliseconds,
                    context.Response.ContentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API Error {RequestId}: {Message}", requestId, ex.Message);
                throw;
            }
            finally
            {
                context.Response.Body = originalResponseBody;
            }
        }
    }
}