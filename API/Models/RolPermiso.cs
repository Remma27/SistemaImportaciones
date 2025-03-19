using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models
{
    public class RolPermiso
    {
        [Key]
        [Required]
        public int id { get; set; }
        
        [Required]
        public int rol_id { get; set; }
        
        [Required]
        public int permiso_id { get; set; }
        
        [ForeignKey("rol_id")]
        public virtual Rol? Rol { get; set; }
        
        [ForeignKey("permiso_id")]
        public virtual Permiso? Permiso { get; set; }
    }
}
