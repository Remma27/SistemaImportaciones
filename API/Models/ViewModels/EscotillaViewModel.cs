namespace Sistema_de_Gestion_de_Importaciones.ViewModels
{
    public class EscotillaViewModel
    {
        public int NumeroEscotilla { get; set; }
        public decimal CapacidadKg { get; set; }
        public decimal DescargaEsperadaKg { get; set; }
        public decimal DescargaRealKg { get; set; }
        public decimal DiferenciaKg { get; set; }
        public decimal Porcentaje { get; set; }
        public string Estado { get; set; } = "Sin Iniciar";
    }
}