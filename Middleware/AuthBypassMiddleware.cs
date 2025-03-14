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

            // If we're bypassing authentication or this is a login/register path
            if (bypassAuth || path.Contains("/auth/") || path.Contains("/tempauth/") || path.StartsWith("/status"))
            {
                _logger.LogInformation("Bypassing authentication for path: {Path}", path);

                // Check for authenticated user claim if needed for views
                if (!(context.User.Identity?.IsAuthenticated ?? false) && path != "/auth/iniciarsesion" && path != "/auth/registrarse")
                {
                    // Add a temporary identity for testing if needed
                    // This would allow views that expect an authenticated user to still render
                    // We're not implementing this here as it might cause other issues
                }
            }

            await _next(context);
        }
    }

    // Extension method for easier middleware registration
    public static class AuthBypassMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuthBypass(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthBypassMiddleware>();
        }
    }
}
