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

            if (!string.IsNullOrEmpty(_policy.ContentSecurityPolicy))
            {
                context.Response.Headers.Append("Content-Security-Policy", _policy.ContentSecurityPolicy);
            }

            if (!string.IsNullOrEmpty(_policy.ReferrerPolicy))
            {
                context.Response.Headers.Append("Referrer-Policy", _policy.ReferrerPolicy);
            }

            if (!string.IsNullOrEmpty(_policy.PermissionsPolicy))
            {
                context.Response.Headers.Append("Permissions-Policy", _policy.PermissionsPolicy);
            }

            context.Response.Headers.Append("Content-Security-Policy",
                "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self'; font-src 'self'; connect-src 'self'");

            await _next(context);
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