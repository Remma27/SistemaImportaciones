using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Sistema_de_Gestion_de_Importaciones.Handlers
{
    public class ApiAuthenticationHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ApiAuthenticationHandler> _logger;

        public ApiAuthenticationHandler(IHttpContextAccessor httpContextAccessor, ILogger<ApiAuthenticationHandler> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var context = _httpContextAccessor.HttpContext;

            if (context != null)
            {
                if (context.Request.Cookies.ContainsKey(".AspNetCore.Cookies") &&
                    context.User?.Identity?.IsAuthenticated != true)
                {
                    _logger.LogWarning("Detectada cookie expirada en ApiAuthenticationHandler");
                }
                else if (context.User?.Identity?.IsAuthenticated == true)
                {
                    var cookies = context.Request.Cookies;
                    if (cookies.ContainsKey(".AspNetCore.Cookies"))
                    {
                        request.Headers.Add("Cookie", $".AspNetCore.Cookies={cookies[".AspNetCore.Cookies"]}");
                    }
                }
            }

            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && context != null)
            {
                _logger.LogWarning("API devolvió 401 - posible sesión expirada");
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }

            return response;
        }
    }
}