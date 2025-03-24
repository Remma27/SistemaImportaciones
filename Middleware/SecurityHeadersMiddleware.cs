using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Sistema_de_Gestion_de_Importaciones.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            
            await _next(context);
        }
    }

    public class SecurityHeadersPolicy
    {
        public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>();

        public SecurityHeadersPolicy()
        {
            Headers["X-Content-Type-Options"] = "nosniff";
            Headers["X-Frame-Options"] = "DENY";
            Headers["X-XSS-Protection"] = "1; mode=block";
            Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            Headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.datatables.net https://cdnjs.cloudflare.com https://cdn.jsdelivr.net https://cdn.sheetjs.com; " +
                "style-src 'self' 'unsafe-inline' https://cdn.datatables.net https://cdnjs.cloudflare.com https://cdn.jsdelivr.net; " +
                "img-src 'self' data:; " +
                "font-src 'self' https://cdnjs.cloudflare.com; " +
                "connect-src 'self'";
        }

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

    public class CspFixMiddleware
    {
        private readonly RequestDelegate _next;

        public CspFixMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            if (context.Response.Headers.ContainsKey("Content-Security-Policy"))
            {
                context.Response.Headers["Content-Security-Policy"] =
                    "default-src 'self'; " +
                    "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.datatables.net https://cdnjs.cloudflare.com https://cdn.jsdelivr.net https://cdn.sheetjs.com; " +
                    "style-src 'self' 'unsafe-inline' https://cdn.datatables.net https://cdnjs.cloudflare.com https://cdn.jsdelivr.net; " +
                    "img-src 'self' data:; " +
                    "font-src 'self' https://cdnjs.cloudflare.com; " +
                    "connect-src 'self'";
            }
        }
    }
}