using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public class Empresa
    {
        [Key]
        public int id_empresa { get; set; }
        public string? nombreempresa { get; set; }
        public int? estatus { get; set; }
        public int? idusuario { get; set; }

    }
}
