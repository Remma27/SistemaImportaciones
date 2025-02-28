namespace Sistema_de_Gestion_de_Importaciones.Models.ViewModels;

public class InformeGeneralViewModel
{
    public int EmpresaId { get; set; }
    public string Empresa { get; set; } = string.Empty;
    public double RequeridoKg { get; set; }
    public double RequeridoTon { get; set; }
    public double DescargaKg { get; set; }
    public double FaltanteKg { get; set; }
    public double TonFaltantes { get; set; }
    public double CamionesFaltantes { get; set; }
    public int ConteoPlacas { get; set; }
    public double PorcentajeDescarga { get; set; }
}