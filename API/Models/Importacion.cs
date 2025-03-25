using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models
{
    public class Importacion
    {
        [Key]
        [Required]
        public int id { get; set; }
        public DateTime? fechahorasystema { get; set; }
        public DateTime? fechahora { get; set; }
        public int? idbarco { get; set; }

        [ForeignKey("idbarco")]
        public virtual Barco? Barco { get; set; }

        public double? totalcargakilos { get; set; }
        public int? idusuario { get; set; }
    }
}
