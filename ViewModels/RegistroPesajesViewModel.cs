using System.Collections.Generic;
using System.Linq;
using API.Models;

namespace Sistema_de_Gestion_de_Importaciones.ViewModels
{
    public class RegistroPesajesViewModel
    {
        // Datos individuales de cada movimiento (Tabla 1)
        public List<RegistroPesajesIndividual> Tabla1Data { get; set; } = new();

        // Datos agregados de informe (Tabla 2)
        public List<RegistroPesajesAgregado> Tabla2Data { get; set; } = new();

        public List<TotalesPorBodegaViewModel> TotalesPorBodega { get; set; } = new();

        public required List<EscotillaViewModel> EscotillasData { get; set; }
        public Barco? Barco { get; set; }
        public Dictionary<int, decimal> DescargaPorEscotilla { get; set; } = new();
        public double? TotalCargaKilos { get; set; }
        public decimal TotalRequerido => Tabla2Data.Sum(x => x.KilosRequeridos);
        public decimal TotalDescargado => Tabla2Data.Sum(x => x.DescargaKilos);
        public decimal TotalFaltante => TotalRequerido - TotalDescargado;
        public decimal FaltanteTotal { get; set; }
        public decimal PorcentajeTotal { get; set; }

        public decimal TotalKilosRequeridos { get; set; }
        public decimal TotalDescargaKilos { get; set; }
        public decimal TotalKilosFaltantes { get; set; }
    }

    public class RegistroPesajesIndividual
    {
        public int Id { get; set; }
        public string Bodega { get; set; } = string.Empty;
        public string Guia { get; set; } = string.Empty;
        public string Placa { get; set; } = string.Empty;
        public decimal PesoRequerido { get; set; }
        public decimal PesoEntregado { get; set; }
        public decimal PesoFaltante { get; set; }
        public decimal Porcentaje { get; set; }

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

    public class EscotillaViewModel
    {
        public int Numero { get; set; }
        public decimal Capacidad { get; set; }
        public decimal DescargaEsperada { get; set; }
        public decimal DescargaReal { get; set; }
        public decimal Diferencia { get; set; }
        public decimal Porcentaje { get; set; }
        public bool EsSobrante { get; set; }
    }
}