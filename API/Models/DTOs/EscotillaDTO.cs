namespace Sistema_de_Gestion_de_Importaciones.Models
{
    public class EscotillaDTO
    {
        public int NumeroEscotilla { get; set; }
        public decimal CapacidadKg { get; set; }
        public decimal DescargaRealKg { get; set; }
        public decimal DiferenciaKg { get; set; }
        public decimal Porcentaje { get; set; }
        public required string Estado { get; set; }
    }
}
