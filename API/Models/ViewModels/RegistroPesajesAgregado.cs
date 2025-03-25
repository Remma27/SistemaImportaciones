namespace Sistema_de_Gestion_de_Importaciones.ViewModels
{
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
}
