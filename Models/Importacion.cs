using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_de_Gestion_de_Importaciones.Models;

public class Importacion
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required(ErrorMessage = "La fecha y hora del sistema es requerida")]
    [Display(Name = "Fecha y Hora del Sistema")]
    [Column("fechahorasystema")]
    public DateTime FechaHoraSystema { get; set; }

    [Required(ErrorMessage = "La fecha y hora es requerida")]
    [Display(Name = "Fecha y Hora")]
    [Column("fechahora")]
    public DateTime FechaHora { get; set; }

    [Required(ErrorMessage = "El ID del barco es requerido")]
    [Display(Name = "ID del Barco")]
    [Column("idbarco")]
    public int IdBarco { get; set; }

    [ForeignKey("IdBarco")]
    public virtual Barco? Barco { get; set; }

    [Required(ErrorMessage = "El total carga kilos es requerido")]
    [Display(Name = "Total Carga Kilos")]
    [Range(1, int.MaxValue, ErrorMessage = "El total de carga debe ser mayor a 0")]
    [Column("totalcargakilos")]
    public double TotalCargaKilos { get; set; }

    // Constructor
    public Importacion()
    {
        FechaHoraSystema = DateTime.Now;
        FechaHora = DateTime.Now;
    }
}