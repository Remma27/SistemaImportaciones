using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_de_Gestion_de_Importaciones.Models
{
    public class Barco
    {
        [Key]
        public int id { get; set; }

        [Required(ErrorMessage = "El nombre del barco es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres")]
        [Display(Name = "Nombre del Barco")]
        public string? nombrebarco { get; set; }

        [Range(0, 999999.99, ErrorMessage = "La capacidad debe estar entre 0 y 999,999.99")]
        [Display(Name = "Escotilla 1")]
        [Column(TypeName = "decimal(10,2)")]
        public decimal? escotilla1 { get; set; }

        [Range(0, 999999.99, ErrorMessage = "La capacidad debe estar entre 0 y 999,999.99")]
        [Display(Name = "Escotilla 2")]
        [Column(TypeName = "decimal(10,2)")]
        public decimal? escotilla2 { get; set; }

        [Range(0, 999999.99, ErrorMessage = "La capacidad debe estar entre 0 y 999,999.99")]
        [Display(Name = "Escotilla 3")]
        [Column(TypeName = "decimal(10,2)")]
        public decimal? escotilla3 { get; set; }

        [Range(0, 999999.99, ErrorMessage = "La capacidad debe estar entre 0 y 999,999.99")]
        [Display(Name = "Escotilla 4")]
        [Column(TypeName = "decimal(10,2)")]
        public decimal? escotilla4 { get; set; }

        [Range(0, 999999.99, ErrorMessage = "La capacidad debe estar entre 0 y 999,999.99")]
        [Display(Name = "Escotilla 5")]
        [Column(TypeName = "decimal(10,2)")]
        public decimal? escotilla5 { get; set; }

        [Range(0, 999999.99, ErrorMessage = "La capacidad debe estar entre 0 y 999,999.99")]
        [Display(Name = "Escotilla 6")]
        [Column(TypeName = "decimal(10,2)")]
        public decimal? escotilla6 { get; set; }

        [Range(0, 999999.99, ErrorMessage = "La capacidad debe estar entre 0 y 999,999.99")]
        [Display(Name = "Escotilla 7")]
        [Column(TypeName = "decimal(10,2)")]
        public decimal? escotilla7 { get; set; }

        public int? idusuario { get; set; }

        [ForeignKey("idusuario")]
        public virtual Usuario? Usuario { get; set; }
    }
}