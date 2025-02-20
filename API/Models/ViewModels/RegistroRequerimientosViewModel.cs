using System.ComponentModel.DataAnnotations;

namespace Sistema_de_Gestion_de_Importaciones.ViewModels
{
    public class RegistroRequerimientosViewModel
    {
        public int IdMovimiento { get; set; }
        public DateTime? FechaHora { get; set; }
        public int? IdImportacion { get; set; }
        public string Importacion { get; set; } = string.Empty;
        public int? IdEmpresa { get; set; }
        public string Empresa { get; set; } = string.Empty;
        public int? TipoTransaccion { get; set; }
        public decimal? CantidadRequerida { get; set; }
        public int? CantidadCamiones { get; set; }
    }
}