using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models
{
    public class Barco
    {
        [Key]
        [Required]
        public int id { get; set; }
        [Required]
        public string? nombrebarco { get; set; }
        public decimal? escotilla1 { get; set; }
        public decimal? escotilla2 { get; set; }
        public decimal? escotilla3 { get; set; }
        public decimal? escotilla4 { get; set; }
        public decimal? escotilla5 { get; set; }
        public decimal? escotilla6 { get; set; }
        public decimal? escotilla7 { get; set; }
        public int? idusuario { get; set; }

        [ForeignKey("idusuario")]
        public virtual Usuario? Usuario { get; set; }

        public Dictionary<int, decimal> ObtenerCapacidadesEscotillas()
        {
            var capacidades = new Dictionary<int, decimal>();
            if (escotilla1 > 0) capacidades.Add(1, (decimal)escotilla1);
            if (escotilla2 > 0) capacidades.Add(2, (decimal)escotilla2);
            if (escotilla3 > 0) capacidades.Add(3, (decimal)escotilla3);
            if (escotilla4 > 0) capacidades.Add(4, (decimal)escotilla4);
            if (escotilla5 > 0) capacidades.Add(5, (decimal)escotilla5);
            if (escotilla6 > 0) capacidades.Add(6, (decimal)escotilla6);
            if (escotilla7 > 0) capacidades.Add(7, (decimal)escotilla7);
            return capacidades;
        }
    }
}
