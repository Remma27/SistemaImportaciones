using System.Diagnostics;

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
