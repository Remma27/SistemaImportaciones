using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public class Permiso
    {
        [Key]
        [Required]
        public int id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string nombre { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string? clave { get; set; }
        
        [StringLength(255)]
        public string? descripcion { get; set; }
        
        // Relación con roles (un permiso puede estar en muchos roles)
        public virtual ICollection<RolPermiso>? RolPermisos { get; set; }
    }
}
