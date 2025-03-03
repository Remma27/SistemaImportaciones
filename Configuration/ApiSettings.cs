using System.ComponentModel.DataAnnotations;

namespace Sistema_de_Gestion_de_Importaciones.Configuration
{
    public class ApiSettings
    {
        [Required]
        public string? BaseUrl { get; set; }

        public int TimeoutSeconds { get; set; } = 30;

        public bool UseAuthentication { get; set; } = true;
    }
}