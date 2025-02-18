using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models
{
    public class Empresa
    {
        [Key]
        [Required]
        public int id_empresa { get; set; }
        [Required]
        public string? nombreempresa { get; set; }
        public int? estatus { get; set; }
        public int? idusuario { get; set; }

        [ForeignKey("idusuario")]
        public virtual Usuario? Usuario { get; set; }
    }
}
