using System.ComponentModel.DataAnnotations;

namespace Sistema_de_Gestion_de_Importaciones.ViewModels
{
    public class RegistroRequerimientosViewModel
    {
        public int? IdMovimiento { get; set; }

        [Required(ErrorMessage = "La fecha y hora son requeridas")]
        [Display(Name = "Fecha y Hora")]
        public DateTime? FechaHora { get; set; }

        [Required(ErrorMessage = "La importación es requerida")]
        [Display(Name = "Importacion")]
        public int? IdImportacion { get; set; }

        [Required(ErrorMessage = "La empresa es requerida")]
        [Display(Name = "Empresa")]
        public int IdEmpresa { get; set; }

        public int? TipoTransaccion { get; set; }

        [Required(ErrorMessage = "La cantidad requerida es obligatoria")]
        [Display(Name = "Cantidad Requerida")]
        [Range(0.01, double.MaxValue, ErrorMessage = "La cantidad requerida debe ser mayor a 0")]
        public decimal CantidadRequerida { get; set; }

        [Required(ErrorMessage = "La cantidad de camiones es obligatoria")]
        [Display(Name = "Cantidad de Camiones")]
        //[Range(1, int.MaxValue, ErrorMessage = "La cantidad de camiones debe ser al menos 1")]
        public int CantidadCamiones { get; set; }
        public string Importacion { get; set; } = string.Empty;
        public string Empresa { get; set; } = string.Empty;
    }
}