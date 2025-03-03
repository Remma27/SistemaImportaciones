namespace API.Models
{
    // Clase DTO para los datos de movimientos
    public class MovimientosCumulatedDto
    {
        public int Id { get; set; }
        public string? Bodega { get; set; }
        public string? Guia { get; set; }
        public string? GuiaAlterna { get; set; }
        public string? Placa { get; set; }
        public string? PlacaAlterna { get; set; }
        public decimal CantidadEntregada { get; set; }
        public decimal CantidadRequerida { get; set; }
        public decimal PesoFaltante { get; set; }
        public decimal Porcentaje { get; set; }
    }
}