// Definición de clases para deserialización directa
using API.Models;

public class MovimientosApiResponse
{
    public List<MovimientosCumulatedDto> Data { get; set; } = new();
    public decimal RequeridoTotal { get; set; }
    public string? Message { get; set; }
}