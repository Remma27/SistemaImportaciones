using System.Security.Claims;
using System.Linq;

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
    }
}
