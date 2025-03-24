using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace Sistema_de_Gestion_de_Importaciones.Middleware
{
    public class RoleDebugMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RoleDebugMiddleware> _logger;
        
        public RoleDebugMiddleware(RequestDelegate next, ILogger<RoleDebugMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        
        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userName = context.User.Identity.Name;
                var roles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
                
                _logger.LogInformation(
                    "Usuario: {UserId} ({UserName}), Roles: {Roles}, IsAdmin: {IsAdmin}",
                    userId, 
                    userName, 
                    string.Join(", ", roles), 
                    context.User.IsInRole("Administrador")
                );

                if (roles.Count == 0 && userName == "Christian Ulloa") 
                {
                    var identity = context.User.Identity as ClaimsIdentity;
                    if (identity != null)
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, "Administrador"));
                        identity.AddClaim(new Claim("role", "Administrador"));
                        identity.AddClaim(new Claim("IsAdmin", "true"));
                        _logger.LogWarning("Rol Administrador forzado para {UserName}", userName);
                    }
                }
            }
            
            await _next(context);
        }
    }
    
    public static class RoleDebugMiddlewareExtensions
    {
        public static IApplicationBuilder UseRoleDebug(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RoleDebugMiddleware>();
        }
    }
}
