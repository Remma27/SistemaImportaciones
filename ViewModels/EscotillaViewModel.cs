
// First, add this class at the top level of your project
namespace Sistema_de_Gestion_de_Importaciones.ViewModels
{
    public class EscotillaViewModel2
    {
        public int Numero { get; set; }
        public decimal CapacidadKilos { get; set; }
        public decimal DescargaKilos { get; set; }
        public decimal DiferenciaKilos { get; set; }
        public decimal PorcentajeUtilizado { get; set; }
        public bool EsSobrante { get; set; }
    }
}