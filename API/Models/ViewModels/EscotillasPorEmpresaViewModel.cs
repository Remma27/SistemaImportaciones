namespace Sistema_de_Gestion_de_Importaciones.ViewModels
{
    public class EscotillaPorEmpresaViewModel
    {
        public int NumeroEscotilla { get; set; }
        public decimal DescargaKg { get; set; }
        public decimal DescargaQuintales { get; set; }
        public decimal DescargaLb { get; set; }
        public decimal DescargaTon { get; set; }
    }

    public class EmpresaEscotillasViewModel
    {
        public int IdEmpresa { get; set; }
        public string NombreEmpresa { get; set; } = string.Empty;
        public List<EscotillaPorEmpresaViewModel> Escotillas { get; set; } = new();
    }

    public class ReporteEscotillasPorEmpresaViewModel
    {
        public List<EmpresaEscotillasViewModel> Empresas { get; set; } = new();
    }
}