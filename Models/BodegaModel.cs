using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaDeGestionDeImportaciones.Models
{
    public class Bodega
    {
        [Key]
        public int id { get; set; }
        [Required]
        public string? Nombre { get; set; }
        public int? idusuario { get; set; }

        [ForeignKey("idusuario")]
        public virtual Usuario? Usuario { get; set; }
    }
}