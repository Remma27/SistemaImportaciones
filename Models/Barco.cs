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
    [Column("nombreBarco")]

    public string? NombreBarco { get; set; }

    [Display(Name = "Escotilla 1")]
    [Column("escotilla1")]

    public int? Escotilla1 { get; set; }

    [Display(Name = "Escotilla 2")]
    [Column("escotilla2")]
    public int? Escotilla2 { get; set; }

    [Display(Name = "Escotilla 3")]
    [Column("escotilla3")]
    public int? Escotilla3 { get; set; }

    [Display(Name = "Escotilla 4")]
    [Column("escotilla4")]
    public int? Escotilla4 { get; set; }

    [Display(Name = "Escotilla 5")]
    [Column("escotilla5")]
    public int? Escotilla5 { get; set; }

    [Display(Name = "Escotilla 6")]
    [Column("escotilla6")]
    public int? Escotilla6 { get; set; }

    [Display(Name = "Escotilla 7")]
    [Column("escotilla7")]
    public int? Escotilla7 { get; set; }

    // Constructor
    public Barco()
    {
        NombreBarco = string.Empty;
    }
}