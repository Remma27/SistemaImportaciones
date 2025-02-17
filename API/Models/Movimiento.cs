using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models
{
    public class Movimiento
    {
        [Key]
        public int id { get; set; }
        public DateTime? fechahora { get; set; }
        public DateTime? fechahorasistema { get; set; }

        [ForeignKey("Importacion")]
        public int? idimportacion { get; set; }
        public virtual Importacion? Importacion { get; set; }

        [ForeignKey("Empresa")]
        public int? idempresa { get; set; }
        public virtual Empresa? Empresa { get; set; }

        public int? tipotransaccion { get; set; }
        public int? cantidadcamiones { get; set; }

        public decimal? cantidadrequerida { get; set; }

        public decimal? cantidadentregada { get; set; }
        public string? placa { get; set; }

        public string? placa_alterna { get; set; }
        public int? guia { get; set; }
        public string? guia_alterna { get; set; }

        public int? escotilla { get; set; }

        public double? totalcarga { get; set; }

        public int? bodega { get; set; }
        public int? idusuario { get; set; }
    }
}
