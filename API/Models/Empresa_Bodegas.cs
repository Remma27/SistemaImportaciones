using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public class Empresa_Bodegas
    {
        [Key]
        public int id { get; set; }
        public string? bodega { get; set; }
        public int? idusuario { get; set; }
    }
}
