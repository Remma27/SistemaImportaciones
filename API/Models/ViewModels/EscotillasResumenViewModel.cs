using Sistema_de_Gestion_de_Importaciones.Models.ViewModels;
using System.Collections.Generic;

namespace Sistema_de_Gestion_de_Importaciones.ViewModels
{
    public class EscotillasResumenViewModel
    {
        public List<EscotillaViewModel> Escotillas { get; set; } = new();
        public decimal CapacidadTotal { get; set; }
        public decimal DescargaTotal { get; set; }
        public decimal DiferenciaTotal { get; set; }
        public decimal PorcentajeTotal { get; set; }
        public string EstadoGeneral { get; set; } = string.Empty;
        public decimal TotalKilosRequeridos { get; set; }
        public string NombreBarco { get; set; } = string.Empty; // Agregar esta propiedad

    }

    public class EscotillaResumenViewModel
    {
        public int NumeroEscotilla { get; set; }
        public decimal CapacidadKg { get; set; }
        public decimal DescargaRealKg { get; set; }
        public decimal DiferenciaKg { get; set; }
        public decimal Porcentaje { get; set; }
        public string Estado { get; set; } = string.Empty;
    }
}