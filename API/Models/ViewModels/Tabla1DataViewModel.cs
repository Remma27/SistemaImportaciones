namespace Sistema_de_Gestion_de_Importaciones.ViewModels
{
    public class Tabla1DataViewModel
    {
        public int Id { get; set; }
        public required string Bodega { get; set; }
        public required string Guia { get; set; }
        public required string GuiaAlterna { get; set; }
        public required string Placa { get; set; }
        public required string PlacaAlterna { get; set; }
        public decimal PesoRequerido { get; set; }
        public decimal PesoEntregado { get; set; }
        public decimal PesoFaltante { get; set; }
        public decimal Porcentaje { get; set; }
    }
}
