using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models
{
    public class Movimiento
    {
        [Key]
        [Required]
        public int id { get; set; }

        [Required]
        public DateTime fechahora { get; set; }

        public DateTime? fechahorasistema { get; set; }

        [ForeignKey("Importacion")]
        [Required]
        public int idimportacion { get; set; }
        public virtual Importacion? Importacion { get; set; }

        [ForeignKey("Empresa")]
        [Required]
        public int idempresa { get; set; }
        public virtual Empresa? Empresa { get; set; }

        [Required]
        public int tipotransaccion { get; set; }
        public int? cantidadcamiones { get; set; }

        public decimal? cantidadrequerida { get; set; }

        [Required]
        public decimal cantidadentregada { get; set; }
        public string? placa { get; set; }

        public string? placa_alterna { get; set; }
        public int? guia { get; set; }
        public string? guia_alterna { get; set; }

        public int? escotilla { get; set; }

        public double? totalcarga { get; set; }

        public int? bodega { get; set; }
        public int? idusuario { get; set; }

        [ForeignKey("idusuario")]
        public virtual Usuario? Usuario { get; set; }

        [NotMapped]
        public decimal peso_faltante { get; set; }

        [NotMapped]
        public decimal porcentaje { get; set; }
    }
}
