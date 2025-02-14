using System;
using System.ComponentModel.DataAnnotations;

namespace Sistema_de_Gestion_de_Importaciones.Models
{
    public class RegistroRequerimientos
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Fecha de Registro")]
        public DateTime FechaRegistro { get; set; }

        [Required]
        [Display(Name = "Empresa")]
        public int EmpresaId { get; set; }

        [Required]
        [Display(Name = "Barco")]
        public int BarcoId { get; set; }

        [Required]
        [Display(Name = "Bodega")]
        public int BodegaId { get; set; }

        [Required]
        [Display(Name = "Cantidad Requerida")]
        public decimal CantidadRequerida { get; set; }

        [Display(Name = "Observaciones")]
        public string? Observaciones { get; set; }

        // Navigation properties
        public virtual Empresa? Empresa { get; set; }
        public virtual Barco? Barco { get; set; }
        public virtual Bodega? Bodega { get; set; }
    }
}
