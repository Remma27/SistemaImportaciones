using System.ComponentModel.DataAnnotations;

namespace Sistema_de_Gestion_de_Importaciones.ViewModels
{
    public class RegistroRequerimientosViewModel
    {
        [Required]
        public int IdMovimiento { get; set; }
        [Required]
        public DateTime? FechaHora { get; set; }
        [Required]
        public int? IdImportacion { get; set; }
        [Required]
        public string Importacion { get; set; } = string.Empty;
        [Required]
        public int IdEmpresa { get; set; }
        [Required]
        public string Empresa { get; set; } = string.Empty;
        [Required]
        public int? TipoTransaccion { get; set; }
        [Required]
        public decimal? CantidadRequerida { get; set; }
        [Required]
        public int? CantidadCamiones { get; set; }
    }
}