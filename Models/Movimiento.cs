using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_de_Gestion_de_Importaciones.Models;

public class Movimiento
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required(ErrorMessage = "La fecha y hora es requerida")]
    [Display(Name = "Fecha y Hora")]
    [Column("fechahora")]
    public DateTime FechaHora { get; set; }

    [Required(ErrorMessage = "La fecha y hora del sistema es requerida")]
    [Display(Name = "Fecha y Hora del Sistema")]
    [Column("fechahorasystema")]
    public DateTime FechaHoraSystema { get; set; }

    [Required(ErrorMessage = "El ID de la importación es requerido")]
    [Display(Name = "Importación")]
    [Column("idimportacion")]
    public int IdImportacion { get; set; }

    [ForeignKey("IdImportacion")]
    public virtual Importacion? Importacion { get; set; }

    [Required(ErrorMessage = "El ID de la empresa es requerido")]
    [Display(Name = "Empresa")]
    [Column("idempresa")]
    public int IdEmpresa { get; set; }

    [ForeignKey("IdEmpresa")]
    public virtual Empresa? Empresa { get; set; }

    [Required(ErrorMessage = "El tipo de transacción es requerido")]
    [Display(Name = "Tipo de Transacción")]
    [Range(1, 2, ErrorMessage = "El tipo de transacción debe ser 1 (Entrada) o 2 (Salida)")]
    [Column("tipotransaccion")]
    public int TipoTransaccion { get; set; }

    [Required(ErrorMessage = "La cantidad de camiones es requerida")]
    [Display(Name = "Cantidad de Camiones")]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad de camiones debe ser mayor a 0")]
    [Column("cantidadcamiones")]
    public int CantidadCamiones { get; set; }

    [Required(ErrorMessage = "La cantidad requerida es requerida")]
    [Display(Name = "Cantidad Requerida")]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad requerida debe ser mayor a 0")]
    [Column("cantidadrequerida")]
    public int CantidadRequerida { get; set; }

    [Required(ErrorMessage = "La cantidad entregada es requerida")]
    [Display(Name = "Cantidad Entregada")]
    [Range(0, int.MaxValue, ErrorMessage = "La cantidad entregada debe ser mayor o igual a 0")]
    [Column("cantidadentregada")]
    public int CantidadEntregada { get; set; }

    [Required(ErrorMessage = "La placa es requerida")]
    [Display(Name = "Placa")]
    [Column("placa")]
    public int Placa { get; set; }

    [Display(Name = "Placa Alterna")]
    [Column("placaalterna")]
    public int PlacaAlterna { get; set; }

    [Required(ErrorMessage = "La guía es requerida")]
    [Display(Name = "Guía")]
    [Column("guia")]
    public int Guia { get; set; }

    [Display(Name = "Guía Alterna")]
    [Column("guiaalterna")]
    public int GuiaAlterna { get; set; }

    [Required(ErrorMessage = "La escotilla es requerida")]
    [Display(Name = "Escotilla")]
    [Range(1, 7, ErrorMessage = "La escotilla debe estar entre 1 y 7")]
    [Column("escotilla")]
    public int Escotilla { get; set; }

    [Required(ErrorMessage = "El total de carga es requerido")]
    [Display(Name = "Total Carga")]
    [Range(1, int.MaxValue, ErrorMessage = "El total de carga debe ser mayor a 0")]
    [Column("totalcarga")]
    public int TotalCarga { get; set; }

    [Required(ErrorMessage = "La bodega es requerida")]
    [Display(Name = "Bodega")]
    [Column("bodega")]
    public int Bodega { get; set; }

    // Constructor
    public Movimiento()
    {
        FechaHoraSystema = DateTime.Now;
        FechaHora = DateTime.Now;
        TipoTransaccion = 1;
    }
}