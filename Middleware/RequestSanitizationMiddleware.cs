using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Sistema_de_Gestion_de_Importaciones.Middleware
{

    public class RequestSanitizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestSanitizationMiddleware> _logger;

        private static readonly Regex _sqlInjectionPattern = new Regex(
            @";\s*(select|insert|update|delete|drop|alter|create|rename|truncate)\s+|--\s+",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex _xssPattern = new Regex(
            @"<script.*?>.*?</script>|javascript\s*:|on\w+\s*=\s*['""].*?['""]",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex _pathTraversalPattern = new Regex(
            @"\.{2,}[\/\\]|\.{2,}$",
            RegexOptions.Compiled);

        public RequestSanitizationMiddleware(RequestDelegate next, ILogger<RequestSanitizationMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            try
            {
                if (IsAuthenticationEndpoint(context))
                {
                    _logger.LogDebug("Skipping sanitization for authentication endpoint: {Path}", context.Request.Path);
                    await _next(context);
                    return;
                }

                if (IsStaticFile(context))
                {
                    await _next(context);
                    return;
                }

                if (context.Request.QueryString.HasValue)
                {
                    if (!SanitizeQueryString(context))
                    {
                        return;
                    }
                }

                if (IsJsonRequest(context))
                {
                    await HandleJsonRequestAsync(context);
                    return;
                }

                if (context.Request.HasFormContentType)
                {
                    if (!await SanitizeFormAsync(context))
                    {
                        return;
                    }
                }

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in request sanitization middleware");

                await _next(context);
            }
        }

        private bool IsAuthenticationEndpoint(HttpContext context)
        {
            var path = context.Request.Path.ToString().ToLowerInvariant();
            return path.Contains("/auth/") ||
                   path.Contains("/login") ||
                   path.Contains("/iniciarsesion") ||
                   path.EndsWith("/token");
        }

        private bool IsStaticFile(HttpContext context)
        {
            var path = context.Request.Path.ToString().ToLowerInvariant();
            return path.StartsWith("/lib/") ||
                   path.StartsWith("/css/") ||
                   path.StartsWith("/js/") ||
                   path.StartsWith("/images/") ||
                   path.StartsWith("/fonts/") ||
                   path.EndsWith(".css") ||
                   path.EndsWith(".js") ||
                   path.EndsWith(".jpg") ||
                   path.EndsWith(".jpeg") ||
                   path.EndsWith(".png") ||
                   path.EndsWith(".gif") ||
                   path.EndsWith(".ico") ||
                   path.EndsWith(".svg") ||
                   path.EndsWith(".woff") ||
                   path.EndsWith(".woff2") ||
                   path.EndsWith(".ttf") ||
                   path.EndsWith(".eot");
        }

        private bool IsJsonRequest(HttpContext context)
        {
            return context.Request.ContentType?.ToLower().Contains("application/json") == true;
        }

        private bool SanitizeQueryString(HttpContext context)
        {
            var originalQuery = context.Request.QueryString.Value;

            if (string.IsNullOrEmpty(originalQuery))
                return true;

            var queryCollection = HttpUtility.ParseQueryString(originalQuery);

            foreach (var key in queryCollection.AllKeys)
            {
                if (key == null) continue;

                var values = queryCollection.GetValues(key);
                if (values == null) continue;

                foreach (var value in values)
                {
                    if (value != null && (ContainsSqlInjection(value) || ContainsXss(value)))
                    {
                        _logger.LogWarning("Potentially malicious query parameter detected: {Key}={Value}", key, value);
                    }
                }
            }

            return true;
        }

        private async Task<bool> SanitizeFormAsync(HttpContext context)
        {
            try
            {
                var form = await context.Request.ReadFormAsync();

                foreach (var key in form.Keys)
                {
                    var values = form[key];
                    foreach (var value in values)
                    {
                        if (value != null && (ContainsSqlInjection(value) || ContainsXss(value)))
                        {
                            _logger.LogWarning("Potentially suspicious form data detected but allowing: {Key}", key);
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing form data in sanitization middleware");
                return true;
            }
        }

        private async Task HandleJsonRequestAsync(HttpContext context)
        {
            var originalBodyStream = context.Request.Body;

            try
            {
                context.Request.EnableBuffering();

                using (var reader = new StreamReader(
                    context.Request.Body,
                    encoding: Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    leaveOpen: true))
                {
                    var body = await reader.ReadToEndAsync();

                    context.Request.Body.Position = 0;

                    if (ContainsSqlInjection(body) || ContainsXss(body))
                    {
                        _logger.LogWarning("Potentially suspicious JSON content detected but allowing");
                    }
                }

                await _next(context);
            }
            finally
            {
                if (context.Request.Body.CanSeek)
                {
                    context.Request.Body.Position = 0;
                }
            }
        }

        private bool ContainsSqlInjection(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            return _sqlInjectionPattern.IsMatch(input);
        }

        private bool ContainsXss(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            return _xssPattern.IsMatch(input);
        }

        private bool ContainsPathTraversal(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            return _pathTraversalPattern.IsMatch(input);
        }
    }
}