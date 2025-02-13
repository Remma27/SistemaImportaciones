using System.Security.Claims;

namespace Sistema_de_Gestion_de_Importaciones.Helpers
{
    public static class UserTrackingExtensions
    {
        public static int GetUserId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst("UserId");
            return claim != null && int.TryParse(claim.Value, out int userId) ? userId : 0;
        }
    }
}
