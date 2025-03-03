using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using API.Models;

namespace Sistema_de_Gestion_de_Importaciones.ViewModels
{
    public class RegistroPesajesViewModel
    {
        public List<RegistroPesajesIndividual> Tabla1Data { get; set; } = new();
        public List<RegistroPesajesAgregado> Tabla2Data { get; set; } = new();
        public List<TotalesPorBodegaViewModel> TotalesPorBodega { get; set; } = new();
        public List<EscotillaViewModel> EscotillasData { get; set; } = new();
        public decimal CapacidadTotal { get; set; }
        public decimal DescargaTotal { get; set; }
        public decimal DiferenciaTotal { get; set; }
        public decimal PorcentajeTotal { get; set; }
        public string EstadoGeneral { get; set; } = string.Empty;

        public Barco? Barco { get; set; }
        public Dictionary<int, decimal> DescargaPorEscotilla { get; set; } = new();
        public double? TotalCargaKilos { get; set; }
        public decimal TotalRequerido => Tabla2Data.Sum(x => x.KilosRequeridos);
        public decimal TotalDescargado => Tabla2Data.Sum(x => x.DescargaKilos);
        public decimal TotalFaltante => TotalRequerido - TotalDescargado;
        public decimal FaltanteTotal { get; set; }
        public decimal TotalKilosRequeridos { get; set; }
        public decimal TotalDescargaKilos { get; set; }
        public decimal TotalKilosFaltantes { get; set; }
    }

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

        [Required(ErrorMessage = "La bodega es requerida")]
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

    public class RegistroPesajesAgregado
    {
        public string Agroindustria { get; set; } = string.Empty;
        public decimal KilosRequeridos { get; set; }
        public decimal ToneladasRequeridas { get; set; }
        public decimal DescargaKilos { get; set; }
        public decimal FaltanteKilos { get; set; }
        public decimal ToneladasFaltantes { get; set; }
        public decimal CamionesFaltantes { get; set; }
        public int ConteoPlacas { get; set; }
        public decimal PorcentajeDescarga { get; set; }

    }

    public class TotalesPorBodegaViewModel
    {
        public string Bodega { get; set; } = string.Empty;
        public decimal TotalKilos { get; set; }
        public int CantidadMovimientos { get; set; }

    }
}