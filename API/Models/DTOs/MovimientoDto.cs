using System;

namespace Sistema_de_Gestion_de_Importaciones.Models.DTOs
{
    public class MovimientoDto
    {
        public int Id { get; set; }
        public DateTime FechaHora { get; set; }
        public int Escotilla { get; set; }
        public int IdEmpresa { get; set; }
        public string? Empresa { get; set; }
        public int? IdBodega { get; set; }
        public string? Bodega { get; set; }
        public int? Guia { get; set; }
        public string? GuiaAlterna { get; set; }
        public string? Placa { get; set; }
        public string? PlacaAlterna { get; set; }
        public decimal PesoEntregadoKg { get; set; }
        public decimal CantidadRetirarKg { get; set; }
        public decimal CantidadRetiradaKg { get; set; }
        public int TipoTransaccion { get; set; }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }
}