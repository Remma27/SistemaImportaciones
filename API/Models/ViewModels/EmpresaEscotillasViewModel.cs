namespace Sistema_de_Gestion_de_Importaciones.ViewModels
{
    public class EmpresaEscotillasViewModel
    {
        public int IdEmpresa { get; set; }
        public string NombreEmpresa { get; set; } = string.Empty;
        public List<EscotillaPorEmpresaViewModel> Escotillas { get; set; } = new();
    }
}
