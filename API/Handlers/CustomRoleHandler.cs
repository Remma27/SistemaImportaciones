using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using System.Threading.Tasks;

namespace API.Handlers
{
    public class CustomRoleHandler : AuthorizationHandler<RolesAuthorizationRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RolesAuthorizationRequirement requirement)
        {
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                return Task.CompletedTask;
            }

            // Buscar cualquier tipo de claim que contenga role o rol
            foreach (var claim in context.User.Claims)
            {
                // Verificar directamente el tipo y valor de cada claim
                if (claim.Type.Contains("role", System.StringComparison.OrdinalIgnoreCase) ||
                    claim.Type.EndsWith("role", System.StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var role in requirement.AllowedRoles)
                    {
                        // Comprobar si el valor del claim coincide con algún rol requerido (ignorando mayúsculas/minúsculas)
                        if (string.Equals(claim.Value, role, System.StringComparison.OrdinalIgnoreCase))
                        {
                            context.Succeed(requirement);
                            return Task.CompletedTask;
                        }
                    }
                }

                // Buscar el claim específico "IsAdmin" para administradores
                if (claim.Type == "IsAdmin" && claim.Value == "true" && 
                    requirement.AllowedRoles.Contains("Administrador"))
                {
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }
            }

            return Task.CompletedTask;
        }
    }
}
