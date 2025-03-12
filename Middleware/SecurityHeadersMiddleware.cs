using Microsoft.AspNetCore.Http;

namespace Sistema_de_Gestion_de_Importaciones.Middleware
{
    public class SecurityHeadersPolicy
    {
        public string? FrameOptions { get; set; }
        public string? ContentTypeOptions { get; set; }
        public string? XssProtection { get; set; }
        public string? ContentSecurityPolicy { get; set; }
        public string? ReferrerPolicy { get; set; }
        public string? PermissionsPolicy { get; set; }
    }

    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly SecurityHeadersPolicy _policy;

        public SecurityHeadersMiddleware(RequestDelegate next, SecurityHeadersPolicy policy)
        {
            _next = next;
            _policy = policy;
        }

        public async Task Invoke(HttpContext context)
        {
            // Skip security headers for static files
            if (IsStaticFile(context.Request.Path))
            {
                await _next(context);
                return;
            }

            if (!string.IsNullOrEmpty(_policy.FrameOptions))
            {
                context.Response.Headers.Append("X-Frame-Options", _policy.FrameOptions);
            }

            if (!string.IsNullOrEmpty(_policy.ContentTypeOptions))
            {
                context.Response.Headers.Append("X-Content-Type-Options", _policy.ContentTypeOptions);
            }

            if (!string.IsNullOrEmpty(_policy.XssProtection))
            {
                context.Response.Headers.Append("X-XSS-Protection", _policy.XssProtection);
            }

            // Establecer una CSP muy permisiva para desarrollo
            // En producción, debe ser más restrictiva
            context.Response.Headers.Append("Content-Security-Policy",
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval' https: http:; " +
                "style-src 'self' 'unsafe-inline' https: http:; " +
                "img-src 'self' data: https: http:; " +
                "font-src 'self' data: https: http:; " +
                "connect-src 'self' https: http:; " +
                "frame-src 'self' https: http:; " +
                "object-src 'none'"
            );

            if (!string.IsNullOrEmpty(_policy.ReferrerPolicy))
            {
                context.Response.Headers.Append("Referrer-Policy", _policy.ReferrerPolicy);
            }

            if (!string.IsNullOrEmpty(_policy.PermissionsPolicy))
            {
                context.Response.Headers.Append("Permissions-Policy", _policy.PermissionsPolicy);
            }

            await _next(context);
        }

        private bool IsStaticFile(PathString path)
        {
            string pathStr = path.ToString().ToLowerInvariant();
            return pathStr.StartsWith("/lib/") ||
                   pathStr.StartsWith("/css/") ||
                   pathStr.StartsWith("/js/") ||
                   pathStr.StartsWith("/images/") ||
                   pathStr.EndsWith(".css") ||
                   pathStr.EndsWith(".js") ||
                   pathStr.EndsWith(".png") ||
                   pathStr.EndsWith(".jpg") ||
                   pathStr.EndsWith(".jpeg") ||
                   pathStr.EndsWith(".gif") ||
                   pathStr.EndsWith(".ico") ||
                   pathStr.EndsWith(".woff") ||
                   pathStr.EndsWith(".woff2");
        }
    }

    public static class SecurityHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeadersMiddleware(this IApplicationBuilder builder, SecurityHeadersPolicy policy)
        {
            return builder.UseMiddleware<SecurityHeadersMiddleware>(policy);
        }
    }
}