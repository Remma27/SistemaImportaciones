using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Sistema_de_Gestion_de_Importaciones.Middleware
{
    public class AuthDebugMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthDebugMiddleware> _logger;

        public AuthDebugMiddleware(RequestDelegate next, ILogger<AuthDebugMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var authDebug = true;
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

            if (authDebug)
            {
                _logger.LogInformation("AUTH_DEBUG: Request path: {Path}, IsAuthenticated: {IsAuthenticated}",
                    path, context.User?.Identity?.IsAuthenticated ?? false);

                if ((path.EndsWith("/auth/iniciarsesion") || path.EndsWith("/auth/login")) &&
                    context.User?.Identity?.IsAuthenticated == true)
                {
                    _logger.LogWarning("Usuario ya autenticado intentando acceder a login. Redirigiendo a home");
                    context.Response.Redirect("/", permanent: false);
                    return;
                }
            }

            await _next(context);
        }
    }

    public static class AuthDebugMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuthDebug(this IApplicationBuilder app)
        {
            return app.UseMiddleware<AuthDebugMiddleware>();
        }
    }
}
