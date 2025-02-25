namespace Sistema_de_Gestion_de_Importaciones.ViewModels
{
    public class EscotillasResponse
    {
        public List<EscotillaViewModel> escotillas { get; set; } = new();
        public EscotillasTotales totales { get; set; } = new();
    }

    public class EscotillasTotales
    {
        public decimal capacidadTotal { get; set; }
        public decimal descargaTotal { get; set; }
        public decimal diferenciaTotal { get; set; }
        public decimal porcentajeTotal { get; set; }
        public string estadoGeneral { get; set; } = string.Empty;
    }
}
