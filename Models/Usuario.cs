using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_de_Gestion_de_Importaciones.Models
{
    [Table("usuarios")]
    public class Usuario
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("nombre")]
        [StringLength(100)]
        public required string Nombre { get; set; }

        [Required]
        [Column("email")]
        [StringLength(100)]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [Column("password_hash")]
        [StringLength(255)]
        public required string PasswordHash { get; set; }

        [Required]
        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Column("ultimo_acceso")]
        public DateTime? UltimoAcceso { get; set; }

        [Column("activo")]
        public bool Activo { get; set; } = true;
    }
}