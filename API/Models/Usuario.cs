using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public class Usuario
    {
        [Key]
        public int id { get; set; }
        public string? nombre { get; set; }
        public string? email { get; set; }
        public string? password_hash { get; set; }
        public DateTime? fecha_creacion { get; set; }
        public DateTime? ultimo_acceso { get; set; }
        public bool? activo { get; set; }
    }
}
