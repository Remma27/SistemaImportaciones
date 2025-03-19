using System.ComponentModel.DataAnnotations;

namespace Sistema_de_Gestion_de_Importaciones.ViewModels
{
    public class AsignarRolViewModel
    {
        public int UsuarioId { get; set; }
        
        [Required(ErrorMessage = "Debe seleccionar un rol")]
        [Display(Name = "Rol")]
        public int RolId { get; set; }
    }
}
