using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_de_Gestion_de_Importaciones.Models;

[Table("empresa_bodegas")]
public class Bodega
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int IdBodega { get; set; }

    [Required(ErrorMessage = "El nombre de la bodega es requerido")]
    [Display(Name = "Nombre de la Bodega")]
    [StringLength(100)]
    [Column("bodega")]
    public string? NombreBodega { get; set; }
}