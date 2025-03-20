using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;

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
            // Lógica antes de la llamada al próximo middleware
            var originalBodyStream = context.Response.Body;

            try 
            {
                using var memoryStream = new MemoryStream();
                context.Response.Body = memoryStream;

                // Continuar con la pipeline
                await _next(context);

                // Procesar la respuesta
                memoryStream.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();
                
                // Sólo procesamos respuestas JSON
                if (context.Response.ContentType?.Contains("application/json") == true)
                {
                    if (responseBody.StartsWith("{") && responseBody.Contains("\"value\":"))
                    {
                        // Analizamos si es una respuesta API que contiene un "value"
                        try
                        {
                            using JsonDocument document = JsonDocument.Parse(responseBody);
                            if (document.RootElement.TryGetProperty("value", out var valueElement))
                            {
                                // Extraer el valor directo
                                var directResponse = valueElement.GetRawText();
                                
                                // Reemplazar el cuerpo con el valor directo
                                memoryStream.SetLength(0);
                                using var writer = new StreamWriter(memoryStream, leaveOpen: true);
                                await writer.WriteAsync(directResponse);
                                await writer.FlushAsync();
                                
                                // Ajustar Content-Length para la respuesta transformada
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

                // Copiar la respuesta final al stream original
                memoryStream.Seek(0, SeekOrigin.Begin);
                await memoryStream.CopyToAsync(originalBodyStream);
            }
            finally
            {
                // Asegurarse de restaurar el stream original
                context.Response.Body = originalBodyStream;
            }
        }
    }

    // Extensión para facilitar el registro del middleware
    public static class StandardResponseMiddlewareExtensions
    {
        public static IApplicationBuilder UseStandardResponse(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<StandardResponseMiddleware>();
        }
    }
}
