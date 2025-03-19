using System.ComponentModel.DataAnnotations;

namespace Sistema_de_Gestion_de_Importaciones.ViewModels
{
    public class CambiarPasswordViewModel
    {
        public int UsuarioId { get; set; }
        
        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} caracteres de longitud.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva contraseña")]
        public string Password { get; set; } = string.Empty;
        
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        [Compare("Password", ErrorMessage = "La contraseña y la confirmación no coinciden.")]
        public string ConfirmPassword { get; set; } = string.Empty;
        
        public string? NombreUsuario { get; set; }
        public string? Email { get; set; }
    }
}
