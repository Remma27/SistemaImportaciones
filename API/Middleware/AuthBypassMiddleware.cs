using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Sistema_de_Gestion_de_Importaciones.Middleware
{
    public class AuthBypassMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthBypassMiddleware> _logger;

        public AuthBypassMiddleware(RequestDelegate next, ILogger<AuthBypassMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.ToString().ToLowerInvariant();
            var bypassAuth = Environment.GetEnvironmentVariable("BYPASS_AUTH") == "true";

            if (bypassAuth || path.Contains("/auth/") || path.Contains("/tempauth/") || path.StartsWith("/status"))
            {
                _logger.LogInformation("Bypassing authentication for path: {Path}", path);

                if (!(context.User.Identity?.IsAuthenticated ?? false) && path != "/auth/iniciarsesion" && path != "/auth/registrarse")
                {
                    _logger.LogWarning("Usuario no autenticado intentando acceder a recurso protegido. Redirigiendo a login");
                    context.Response.Redirect("/auth/iniciarsesion", permanent: false);
                    return;
                }
            }

            await _next(context);
        }
    }

    public static class AuthBypassMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuthBypass(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthBypassMiddleware>();
        }
    }
}
