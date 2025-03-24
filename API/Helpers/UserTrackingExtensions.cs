using System.Security.Claims;
using System.Linq;
using System.Collections.Generic;

namespace Sistema_de_Gestion_de_Importaciones.Helpers
{
    public static class UserTrackingExtensions
    {
        public static int GetUserId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("UserId");
            
            if (claim != null && int.TryParse(claim.Value, out int userId))
            {
                return userId;
            }
            
            var allClaims = string.Join(", ", user.Claims.Select(c => $"{c.Type}={c.Value}"));
            System.Diagnostics.Debug.WriteLine($"Claims disponibles: {allClaims}");
            
            return 0;
        }

        public static string GetUserRole(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst(ClaimTypes.Role);
            
            if (claim != null)
            {
                return claim.Value;
            }
            
            return string.Empty;
        }

        public static bool HasPermission(this ClaimsPrincipal user, string permission)
        {
            if (user.IsInRole("Administrador"))
            {
                return true;
            }
            
            return user.HasClaim(c => c.Type == "Permission" && c.Value == permission);
        }
    }
}
