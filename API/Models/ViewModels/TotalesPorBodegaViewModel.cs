namespace Sistema_de_Gestion_de_Importaciones.ViewModels
{
    public class TotalesPorBodegaViewModel
    {
        public string Bodega { get; set; } = string.Empty;
        public decimal TotalKilos { get; set; }
        public int CantidadMovimientos { get; set; }
    }
}
