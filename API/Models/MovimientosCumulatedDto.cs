namespace API.Models
{
    public class MovimientosCumulatedDto
    {
        public string bodega { get; set; } = string.Empty;
        public string guia { get; set; } = string.Empty;
        public string placa { get; set; } = string.Empty;
        public decimal cantidadrequerida { get; set; }
        public decimal cantidadentregada { get; set; }
        public decimal peso_faltante { get; set; }
        public decimal porcentaje { get; set; }
    }
}