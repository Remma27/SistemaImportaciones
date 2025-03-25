
namespace Sistema_de_Gestion_de_Importaciones.Middleware
{
    public class SessionExpirationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SessionExpirationMiddleware> _logger;

        public SessionExpirationMiddleware(RequestDelegate next, ILogger<SessionExpirationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

            if (path.StartsWith("/auth/") || path == "/auth")
            {
                await _next(context);
                return;
            }

            if (context.Request.Cookies.ContainsKey(".AspNetCore.Cookies") &&
                context.User?.Identity?.IsAuthenticated != true)
            {
                _logger.LogWarning("Cookie de autenticación detectada pero usuario no autenticado - cookie vencida");

                context.Response.Cookies.Delete(".AspNetCore.Cookies");

                if (!path.StartsWith("/api") &&
                    !context.Request.Headers["X-Requested-With"].ToString().Contains("XMLHttpRequest"))
                {
                    context.Response.Redirect("/Auth/IniciarSesion");
                    return;
                }

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            await _next(context);
        }
    }
}