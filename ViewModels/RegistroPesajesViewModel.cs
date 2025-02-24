using System.Collections.Generic;
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
}