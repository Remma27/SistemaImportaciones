using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

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
        
        // Añadir JsonIgnore para evitar referencias circulares
        [JsonIgnore]
        public virtual ICollection<Usuario>? Usuarios { get; set; }
        
        [JsonIgnore]
        public virtual ICollection<RolPermiso>? RolPermisos { get; set; }
    }
}
