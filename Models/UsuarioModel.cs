using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaDeGestionDeImportaciones.Models;

public class Usuario
{
    [Key]
    public int id { get; set; }
    [Required]
    public string? nombre { get; set; }
    [Required]
    public string? email { get; set; }
    [Required]
    public string? password_hash { get; set; }
    public DateTime? fecha_creacion { get; set; }
    public DateTime? ultimo_acceso { get; set; }
    public bool? activo { get; set; }
}