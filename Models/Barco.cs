using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_de_Gestion_de_Importaciones.Models;

public class Barco
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre del barco es requerido")]
    [Display(Name = "Nombre del Barco")]
    [StringLength(100)]
    [Column("nombrebarco")]
    public string? NombreBarco { get; set; }

    [Display(Name = "Escotilla 1")]
    [Column("escotilla1", TypeName = "decimal(15,4)")]
    [DisplayFormat(DataFormatString = "{0:F4}")]
    public decimal? Escotilla1 { get; set; }

    [Display(Name = "Escotilla 2")]
    [Column("escotilla2", TypeName = "decimal(15,4)")]
    [DisplayFormat(DataFormatString = "{0:F4}")]
    public decimal? Escotilla2 { get; set; }

    [Display(Name = "Escotilla 3")]
    [Column("escotilla3", TypeName = "decimal(15,4)")]
    [DisplayFormat(DataFormatString = "{0:F4}")]
    public decimal? Escotilla3 { get; set; }

    [Display(Name = "Escotilla 4")]
    [Column("escotilla4", TypeName = "decimal(15,4)")]
    [DisplayFormat(DataFormatString = "{0:F4}")]
    public decimal? Escotilla4 { get; set; }

    [Display(Name = "Escotilla 5")]
    [Column("escotilla5", TypeName = "decimal(15,4)")]
    [DisplayFormat(DataFormatString = "{0:F4}")]
    public decimal? Escotilla5 { get; set; }

    [Display(Name = "Escotilla 6")]
    [Column("escotilla6", TypeName = "decimal(15,4)")]
    [DisplayFormat(DataFormatString = "{0:F4}")]
    public decimal? Escotilla6 { get; set; }

    [Display(Name = "Escotilla 7")]
    [Column("escotilla7", TypeName = "decimal(15,4)")]
    [DisplayFormat(DataFormatString = "{0:F4}")]
    public decimal? Escotilla7 { get; set; }

    [Display(Name = "Usuario")]
    [Column("idusuario")]
    public int? IdUsuario { get; set; }

    // Constructor
    public Barco()
    {
        NombreBarco = string.Empty;
        Escotilla1 = 0.0m;
        Escotilla2 = 0.0m;
        Escotilla3 = 0.0m;
        Escotilla4 = 0.0m;
        Escotilla5 = 0.0m;
        Escotilla6 = 0.0m;
        Escotilla7 = 0.0m;
    }
}