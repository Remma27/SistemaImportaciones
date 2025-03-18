using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models
{
    [Table("historialcambios")]
    public class HistorialCambios
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Required]
        [Column("usuario_id")]
        public int UsuarioId { get; set; }
        
        [Required]
        [Column("tabla")]
        public string Tabla { get; set; } = string.Empty;
        
        [Required]
        [Column("tipo_operacion")]
        public string TipoOperacion { get; set; } = string.Empty; // "CREATE", "UPDATE", "DELETE"
        
        [Required]
        [Column("datos_json")]
        public string DatosJSON { get; set; } = string.Empty;
        
        [Required]
        [Column("fecha_hora")]
        public DateTime FechaHora { get; set; } = DateTime.Now;
        
        [Column("descripcion")]
        public string? Descripcion { get; set; }
        
        [ForeignKey("UsuarioId")]
        public virtual Usuario? Usuario { get; set; }
    }
}
