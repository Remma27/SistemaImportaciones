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
        public string NombreBarco { get; set; } = string.Empty;

    }
}