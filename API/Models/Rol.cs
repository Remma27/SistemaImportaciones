using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models
{
    public class Rol
    {
        [Key]
        [Required]
        public int id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string nombre { get; set; } = string.Empty;
        
        [StringLength(255)]
        public string? descripcion { get; set; }
        
        // Relación con usuarios (un rol puede tener muchos usuarios)
        public virtual ICollection<Usuario>? Usuarios { get; set; }
        
        // Relación con permisos (un rol puede tener muchos permisos)
        public virtual ICollection<RolPermiso>? RolPermisos { get; set; }
    }
}
