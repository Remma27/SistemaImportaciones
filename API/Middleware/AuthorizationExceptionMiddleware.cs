using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Sistema_de_Gestion_de_Importaciones.Middleware
{
    public class AuthorizationExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthorizationExceptionMiddleware> _logger;

        public AuthorizationExceptionMiddleware(RequestDelegate next, ILogger<AuthorizationExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Sesión expirada o usuario no autenticado");

                if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                    context.Request.Headers["Accept"].ToString().Contains("application/json"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { error = "Sesión expirada", requireLogin = true });
                }
                else
                {
                    context.Response.Redirect("/Auth/IniciarSesion");
                }
            }
        }
    }
}