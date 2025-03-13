using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Sistema_de_Gestion_de_Importaciones.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly SecurityHeadersPolicy _policy;
        private readonly ILogger<SecurityHeadersMiddleware> _logger;

        public SecurityHeadersMiddleware(RequestDelegate next, SecurityHeadersPolicy policy, ILogger<SecurityHeadersMiddleware> logger)
        {
            _next = next;
            _policy = policy;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.ToString().ToLowerInvariant();
            var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

            // Skip security headers for auth paths in development
            if (isDevelopment && (path.Contains("/auth/") || path.StartsWith("/authfix/")))
            {
                _logger.LogInformation("Skipping security headers for auth path: {Path}", path);
                await _next(context);
                return;
            }

            // Aplica los encabezados de seguridad siguiendo la política
            foreach (var headerValuePair in _policy.Headers)
            {
                // Evita sobreescribir encabezados CSP ya definidos
                if (context.Response.Headers.ContainsKey(headerValuePair.Key))
                {
                    if (headerValuePair.Key == "Content-Security-Policy")
                    {
                        // Mantén la entrada existente para CSP
                        continue;
                    }
                    context.Response.Headers.Remove(headerValuePair.Key);
                }
                context.Response.Headers.Append(headerValuePair.Key, headerValuePair.Value);
            }

            await _next(context);
        }
    }

    public class SecurityHeadersPolicy
    {
        public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>();

        public SecurityHeadersPolicy()
        {
            // Configuración base recomendada
            Headers["X-Content-Type-Options"] = "nosniff";
            Headers["X-Frame-Options"] = "DENY";
            Headers["X-XSS-Protection"] = "1; mode=block";
            Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Política CSP actualizada para permitir CDNs específicos
            Headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.datatables.net https://cdnjs.cloudflare.com; " +
                "style-src 'self' 'unsafe-inline' https://cdn.datatables.net https://cdnjs.cloudflare.com; " +
                "img-src 'self' data:; " +
                "font-src 'self' https://cdnjs.cloudflare.com; " +
                "connect-src 'self'";
        }

        // Método para agregar una política CSP personalizada
        public ContentSecurityPolicyBuilder AddContentSecurityPolicy()
        {
            var builder = new ContentSecurityPolicyBuilder();
            return builder;
        }
    }

    public class ContentSecurityPolicyBuilder
    {
        private readonly Dictionary<string, List<string>> _directives = new Dictionary<string, List<string>>();

        public ContentSecurityPolicyDirectiveBuilder AddDefaultSrc() => new ContentSecurityPolicyDirectiveBuilder(this, "default-src");
        public ContentSecurityPolicyDirectiveBuilder AddScriptSrc() => new ContentSecurityPolicyDirectiveBuilder(this, "script-src");
        public ContentSecurityPolicyDirectiveBuilder AddStyleSrc() => new ContentSecurityPolicyDirectiveBuilder(this, "style-src");
        public ContentSecurityPolicyDirectiveBuilder AddImgSrc() => new ContentSecurityPolicyDirectiveBuilder(this, "img-src");
        public ContentSecurityPolicyDirectiveBuilder AddFontSrc() => new ContentSecurityPolicyDirectiveBuilder(this, "font-src");
        public ContentSecurityPolicyDirectiveBuilder AddConnectSrc() => new ContentSecurityPolicyDirectiveBuilder(this, "connect-src");

        public void AddDirective(string directive, string value)
        {
            if (!_directives.ContainsKey(directive))
            {
                _directives[directive] = new List<string>();
            }
            _directives[directive].Add(value);
        }

        // Más métodos según sea necesario
    }

    public class ContentSecurityPolicyDirectiveBuilder
    {
        private readonly ContentSecurityPolicyBuilder _builder;
        private readonly string _directive;

        public ContentSecurityPolicyDirectiveBuilder(ContentSecurityPolicyBuilder builder, string directive)
        {
            _builder = builder;
            _directive = directive;
        }

        public ContentSecurityPolicyDirectiveBuilder Self()
        {
            _builder.AddDirective(_directive, "'self'");
            return this;
        }

        public ContentSecurityPolicyDirectiveBuilder UnsafeInline()
        {
            _builder.AddDirective(_directive, "'unsafe-inline'");
            return this;
        }

        public ContentSecurityPolicyDirectiveBuilder UnsafeEval()
        {
            _builder.AddDirective(_directive, "'unsafe-eval'");
            return this;
        }

        public ContentSecurityPolicyDirectiveBuilder Data()
        {
            _builder.AddDirective(_directive, "data:");
            return this;
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