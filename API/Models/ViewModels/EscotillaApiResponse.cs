namespace Sistema_de_Gestion_de_Importaciones.ViewModels
{
    public class EscotillaApiResponse
    {
        public List<EscotillaDetalle> Escotillas { get; set; } = new();
        public TotalesEscotillas Totales { get; set; } = new();
    }

    public class EscotillaDetalle
    {
        public int NumeroEscotilla { get; set; }
        public decimal CapacidadKg { get; set; }
        public decimal DescargaRealKg { get; set; }
        public decimal DiferenciaKg { get; set; }
        public decimal Porcentaje { get; set; }
        public string Estado { get; set; } = string.Empty;
    }

    public class TotalesEscotillas
    {
        public decimal TotalKilosRequeridos { get; set; } = 0;

        public decimal CapacidadTotal { get; set; }
        public decimal DescargaTotal { get; set; }
        public decimal DiferenciaTotal { get; set; }
        public decimal PorcentajeTotal { get; set; }
        public string EstadoGeneral { get; set; } = string.Empty;
    }
}