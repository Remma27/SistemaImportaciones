// First, add this class at the top level of your project
namespace Sistema_de_Gestion_de_Importaciones.ViewModels
{
    public class EscotillaViewModel
    {
        public int NumeroEscotilla { get; set; }
        public decimal CapacidadKg { get; set; }
        public decimal DescargaEsperadaKg { get; set; }
        public decimal DescargaRealKg { get; set; }
        public decimal DiferenciaKg { get; set; }
        public decimal Porcentaje { get; set; }
        public string Estado { get; set; } = "Sin Iniciar";

        public int Numero
        {
            get => NumeroEscotilla;
            set => NumeroEscotilla = value;
        }

        public decimal Capacidad
        {
            get => CapacidadKg;
            set => CapacidadKg = value;
        }

        public decimal DescargaEsperada
        {
            get => DescargaEsperadaKg;
            set => DescargaEsperadaKg = value;
        }

        public decimal DescargaReal
        {
            get => DescargaRealKg;
            set => DescargaRealKg = value;
        }

        public decimal Diferencia
        {
            get => DiferenciaKg;
            set => DiferenciaKg = value;
        }
    }
}