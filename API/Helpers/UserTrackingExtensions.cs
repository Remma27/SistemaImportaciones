using System.Security.Claims;
using System.Linq;
using System.Collections.Generic;

namespace Sistema_de_Gestion_de_Importaciones.Helpers
{
    public static class UserTrackingExtensions
    {
        public static int GetUserId(this ClaimsPrincipal user)
        {
            // El problema es que estamos buscando "UserId" cuando en realidad 
            // la autenticación de ASP.NET Core guarda el ID como ClaimTypes.NameIdentifier
            var claim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("UserId");
            
            if (claim != null && int.TryParse(claim.Value, out int userId))
            {
                return userId;
            }
            
            // Para depuración: registrar todas las claims disponibles
            var allClaims = string.Join(", ", user.Claims.Select(c => $"{c.Type}={c.Value}"));
            System.Diagnostics.Debug.WriteLine($"Claims disponibles: {allClaims}");
            
            return 0;
        }

        // Método para obtener el rol del usuario
        public static string GetUserRole(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst(ClaimTypes.Role);
            
            if (claim != null)
            {
                return claim.Value;
            }
            
            return string.Empty;
        }

        // Método para verificar si un usuario tiene un permiso específico
        public static bool HasPermission(this ClaimsPrincipal user, string permission)
        {
            // Si el usuario es administrador, tiene todos los permisos
            if (user.IsInRole("Administrador"))
            {
                return true;
            }
            
            // Buscar el permiso en las claims
            return user.HasClaim(c => c.Type == "Permission" && c.Value == permission);
        }
    }
}
