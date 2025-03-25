using System.Text.Json;

namespace API.Middleware
{
    public class StandardResponseMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<StandardResponseMiddleware> _logger;

        public StandardResponseMiddleware(RequestDelegate next, ILogger<StandardResponseMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var originalBodyStream = context.Response.Body;

            try 
            {
                using var memoryStream = new MemoryStream();
                context.Response.Body = memoryStream;

                await _next(context);

                memoryStream.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();
                
                if (context.Response.ContentType?.Contains("application/json") == true)
                {
                    if (responseBody.StartsWith("{") && responseBody.Contains("\"value\":"))
                    {
                        try
                        {
                            using JsonDocument document = JsonDocument.Parse(responseBody);
                            if (document.RootElement.TryGetProperty("value", out var valueElement))
                            {
                                var directResponse = valueElement.GetRawText();
                                
                                memoryStream.SetLength(0);
                                using var writer = new StreamWriter(memoryStream, leaveOpen: true);
                                await writer.WriteAsync(directResponse);
                                await writer.FlushAsync();
                                
                                context.Response.ContentLength = memoryStream.Length;
                                
                                _logger.LogInformation("Respuesta API convertida a formato estándar");
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(ex, "Error al procesar respuesta JSON");
                        }
                    }
                }

                memoryStream.Seek(0, SeekOrigin.Begin);
                await memoryStream.CopyToAsync(originalBodyStream);
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }
    }

    public static class StandardResponseMiddlewareExtensions
    {
        public static IApplicationBuilder UseStandardResponse(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<StandardResponseMiddleware>();
        }
    }
}
