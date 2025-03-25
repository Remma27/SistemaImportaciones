
namespace Sistema_de_Gestion_de_Importaciones.Middleware
{
    public class RedirectUnauthorizedMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RedirectUnauthorizedMiddleware> _logger;

        public RedirectUnauthorizedMiddleware(RequestDelegate next, ILogger<RedirectUnauthorizedMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

            if (IsAllowAnonymousPath(path))
            {
                _logger.LogInformation("Permitiendo acceso anónimo a ruta {Path}", path);
                await _next(context);
                return;
            }

            if (context.User?.Identity?.IsAuthenticated != true)
            {
                _logger.LogWarning("Usuario no autenticado intentando acceder a: {Path}", path);

                if (!IsApiRequest(context.Request) && !IsAjaxRequest(context.Request))
                {
                    context.Response.Redirect($"/Auth/IniciarSesion?returnUrl={System.Net.WebUtility.UrlEncode(path)}");
                    return;
                }

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            await _next(context);
        }

        private bool IsAllowAnonymousPath(string path)
        {
            return path.StartsWith("/auth/") ||
                   path.StartsWith("/auth") ||
                   path == "/" ||
                   path.StartsWith("/css/") ||
                   path.StartsWith("/js/") ||
                   path.StartsWith("/lib/") ||
                   path.StartsWith("/images/") ||
                   path.StartsWith("/favicon") ||
                   path.StartsWith("/health");
        }

        private bool IsApiRequest(HttpRequest request)
        {
            return request.Path.StartsWithSegments("/api");
        }

        private bool IsAjaxRequest(HttpRequest request)
        {
            return request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                  (request.Headers.ContainsKey("Accept") &&
                   request.Headers["Accept"].ToString().Contains("application/json"));
        }
    }
}