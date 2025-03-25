using System.Collections.Generic;
using System.Linq;
using API.Models;

namespace Sistema_de_Gestion_de_Importaciones.ViewModels
{
    public class RegistroPesajesViewModel
    {
        public string? NombreBarco { get; set; }
        public List<RegistroPesajesIndividual> Tabla1Data { get; set; } = new List<RegistroPesajesIndividual>();
        public List<RegistroPesajesAgregado> Tabla2Data { get; set; } = new List<RegistroPesajesAgregado>();
        public List<TotalesPorBodegaViewModel> TotalesPorBodega { get; set; } = new List<TotalesPorBodegaViewModel>();
        public List<EscotillaViewModel> EscotillasData { get; set; } = new List<EscotillaViewModel>();
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
    }
}