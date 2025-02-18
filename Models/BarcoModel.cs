using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaDeGestionDeImportaciones.Models
{
    public class Barco
    {
        [Key]
        public int id { get; set; }
        [Required]
        public string? nombrebarco { get; set; }
        public decimal? escotilla1 { get; set; }
        public decimal? escotilla2 { get; set; }
        public decimal? escotilla3 { get; set; }
        public decimal? escotilla4 { get; set; }
        public decimal? escotilla5 { get; set; }
        public decimal? escotilla6 { get; set; }
        public decimal? escotilla7 { get; set; }
        public int? idusuario { get; set; }

        [ForeignKey("idusuario")]
        public virtual Usuario? Usuario { get; set; }
    }
}