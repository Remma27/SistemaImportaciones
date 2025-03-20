using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace API.Models
{
    public class Usuario
    {
        [Key]
        [Required]
        public int id { get; set; }
        
        [Required]
        public string? nombre { get; set; }
        
        [Required]
        public string? email { get; set; }
        
        // Quitar la anotación [Required] para permitir actualizaciones parciales
        public string? password_hash { get; set; }
        
        public DateTime? fecha_creacion { get; set; }
        public DateTime? ultimo_acceso { get; set; }
        public bool? activo { get; set; }
        public int? rol_id { get; set; }

        [ForeignKey("rol_id")]
        public virtual Rol? Rol { get; set; }
    }
}
