using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
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
            var path = context.Request.Path.ToString().ToLowerInvariant();
            var authDebug = Environment.GetEnvironmentVariable("AUTH_DEBUG") == "true";

            if (authDebug)
            {
                _logger.LogInformation("AUTH_DEBUG: Request path: {Path}, IsAuthenticated: {IsAuthenticated}",
                    path, context.User?.Identity?.IsAuthenticated ?? false);

                // Allow anonymous access to Auth controller endpoints
                if (path.StartsWith("/auth/") || path == "/auth")
                {
                    _logger.LogInformation("AUTH_DEBUG: Allowing anonymous access to Auth controller");

                    // If we're trying to access a protected page and we're not authenticated
                    if (path.Contains("/iniciarsesion") && context.Request.Query.ContainsKey("ReturnUrl"))
                    {
                        // Log the return URL that's causing issues
                        var returnUrl = context.Request.Query["ReturnUrl"];
                        _logger.LogWarning("AUTH_DEBUG: ReturnUrl detected: {ReturnUrl}", returnUrl);
                    }

                    // If we're already authenticated but trying to access the login page, redirect to home
                    if ((path == "/auth/iniciarsesion" || path == "/auth/login") &&
                        context.User?.Identity?.IsAuthenticated == true)
                    {
                        _logger.LogInformation("AUTH_DEBUG: User already authenticated, redirecting to home");
                        context.Response.Redirect("/");
                        return;
                    }
                }
            }

            await _next(context);

            // Log authentication failures
            if (authDebug && context.Response.StatusCode == 401 || context.Response.StatusCode == 403)
            {
                _logger.LogWarning("AUTH_DEBUG: Authentication/Authorization failed for path: {Path}, StatusCode: {StatusCode}",
                    path, context.Response.StatusCode);
            }

            // Log redirects which might indicate auth issues
            if (authDebug &&
                (context.Response.StatusCode == 302 || context.Response.StatusCode == 307) &&
                context.Response.Headers.TryGetValue("Location", out var locationValues))
            {
                _logger.LogInformation("AUTH_DEBUG: Redirect to {Location} for path {Path}",
                    locationValues.FirstOrDefault(), path);
            }
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
