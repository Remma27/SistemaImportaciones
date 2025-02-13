using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_de_Gestion_de_Importaciones.Models;

public class Empresa
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id_empresa")]
    public int IdEmpresa { get; set; }

    [Required(ErrorMessage = "El nombre de la empresa es requerido")]
    [Display(Name = "Nombre de la empresa")]
    [StringLength(100)]
    [Column("nombreempresa")]
    public string? NombreEmpresa { get; set; }

    [Required(ErrorMessage = "El estatus es requerido")]
    [Display(Name = "Estatus")]
    [Range(1, 2, ErrorMessage = "El estatus debe ser 1 o 2")]
    [Column("estatus")]
    public int Estatus { get; set; }

    [Display(Name = "Usuario")]
    [Column("idusuario")]
    public int? IdUsuario { get; set; }

    // Constructor
    public Empresa()
    {
        NombreEmpresa = string.Empty;
        Estatus = 1; // Default to "Activo"
    }
}