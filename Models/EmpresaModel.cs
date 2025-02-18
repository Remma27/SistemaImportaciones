using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_de_Gestion_de_Importaciones.Models;

public class Empresa
{
    [Key]
    public int id_empresa { get; set; }
    [Required]
    public string? nombreempresa { get; set; }
    public int? estatus { get; set; }
    public int? idusuario { get; set; }

    [ForeignKey("idusuario")]
    public virtual Usuario? Usuario { get; set; }
}