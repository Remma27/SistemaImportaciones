using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Sistema_de_Gestion_de_Importaciones.Exceptions;
using System;
using System.Threading.Tasks;

namespace Sistema_de_Gestion_de_Importaciones.Middleware
{
    public class ApiErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiErrorHandlingMiddleware> _logger;

        public ApiErrorHandlingMiddleware(RequestDelegate next, ILogger<ApiErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);

                if (context.Response.StatusCode == 401 &&
                    (context.Request.Path.StartsWithSegments("/api") ||
                     IsAjaxRequest(context.Request)))
                {
                    _logger.LogWarning("API error 401 interceptado para {Path}", context.Request.Path);

                    if (IsAjaxRequest(context.Request))
                    {
                        await HandleAjaxUnauthorized(context);
                    }
                }
            }
            catch (System.Net.Http.HttpRequestException ex) when (ex.Message.Contains("401"))
            {
                _logger.LogWarning("API auth error intercepted: {Message}", ex.Message);

                if (!IsAjaxRequest(context.Request))
                {
                    context.Response.Redirect("/Auth/IniciarSesion");
                    return;
                }

                await HandleAjaxUnauthorized(context);
            }
            catch (SessionExpiredException)
            {
                _logger.LogWarning("Sesión expirada detectada");

                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                if (IsAjaxRequest(context.Request))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"error\":\"session_expired\",\"message\":\"La sesión ha expirado\",\"redirectUrl\":\"/Auth/IniciarSesion\"}");
                }
                else
                {
                    context.Response.Redirect("/Auth/IniciarSesion");
                }
            }
        }

        private bool IsAjaxRequest(HttpRequest request)
        {
            return request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                   request.Headers["Accept"].ToString().Contains("application/json");
        }

        private async Task HandleAjaxUnauthorized(HttpContext context)
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\":\"No autorizado\",\"redirectUrl\":\"/Auth/IniciarSesion\"}");
        }
    }
}