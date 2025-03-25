using System;
using System.ComponentModel.DataAnnotations;

namespace Sistema_de_Gestion_de_Importaciones.ViewModels
{
    public class RegistroPesajesIndividual
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "La importación es requerida")]
        public int IdImportacion { get; set; }

        [Required(ErrorMessage = "La empresa es requerida")]
        public int IdEmpresa { get; set; }

        [Required(ErrorMessage = "La guía es requerida")]
        public string Guia { get; set; } = string.Empty;

        public string? GuiaAlterna { get; set; }

        [Required(ErrorMessage = "La placa es requerida")]
        public string? Placa { get; set; }
        public string? PlacaAlterna { get; set; }

        [Required(ErrorMessage = "El peso es requerido")]
        [Range(1, double.MaxValue, ErrorMessage = "El peso debe ser mayor a 0")]
        public decimal PesoEntregado { get; set; }

        [Required(ErrorMessage = "La escotilla es requerida")]
        [Range(1, 7, ErrorMessage = "La escotilla debe estar entre 1 y 7")]
        public int Escotilla { get; set; }

        public string? Bodega { get; set; }

        public int TipoTransaccion { get; set; } = 2;
        public DateTime FechaHora { get; set; } = DateTime.Now;
        public decimal PesoRequerido { get; internal set; }
        public decimal PesoFaltante { get; set; }
        public decimal Porcentaje { get; set; }
        public string EmpresaNombre { get; set; } = string.Empty;
        public decimal CantidadRetiradaKg { get; internal set; }
        public decimal? CantidadRequeridaQuintales { get; set; }
        public decimal? CantidadEntregadaQuintales { get; set; }
        public decimal? CantidadRequeridaLibras { get; set; }
        public decimal? CantidadEntregadaLibras { get; set; }
    }
}
