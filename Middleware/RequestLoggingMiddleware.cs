using System.Diagnostics;
using System.IO;
using System.Text;

namespace Sistema_de_Gestion_de_Importaciones.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation($"Iniciando solicitud: {context.Request.Method} {context.Request.Path}");

                // For debugging JSON body issues
                if (context.Request.ContentType?.Contains("application/json") == true &&
                    context.Request.Method != "GET")
                {
                    context.Request.EnableBuffering();
                    using (var reader = new StreamReader(
                        context.Request.Body,
                        encoding: Encoding.UTF8,
                        detectEncodingFromByteOrderMarks: false,
                        leaveOpen: true))
                    {
                        var body = await reader.ReadToEndAsync();
                        _logger.LogInformation("Request JSON Body: {Body}", body);

                        // Very important - reset position to start
                        context.Request.Body.Position = 0;
                    }
                }

                await _next(context);
                sw.Stop();
                _logger.LogInformation($"Solicitud completada: {context.Response.StatusCode} en {sw.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en la solicitud");
                throw;
            }
        }
    }
}
