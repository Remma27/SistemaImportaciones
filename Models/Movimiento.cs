using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_de_Gestion_de_Importaciones.Models;

public class Movimiento
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Display(Name = "Fecha y Hora")]
    [Column("fechahora")]
    public DateTime? FechaHora { get; set; }

    [Display(Name = "Fecha y Hora del Sistema")]
    [Column("fechahorasistema")]
    public DateTime? FechaHoraSystema { get; set; }

    [Display(Name = "Importación")]
    [Column("idimportacion")]
    public int? IdImportacion { get; set; }       // changed to nullable

    [ForeignKey("IdImportacion")]
    public virtual Importacion? Importacion { get; set; }

    [Display(Name = "Empresa")]
    [Column("idempresa")]
    public int? IdEmpresa { get; set; }           // changed to nullable

    [ForeignKey("IdEmpresa")]
    public virtual Empresa? Empresa { get; set; }

    [Display(Name = "Tipo de Transacción")]
    [Range(1, 2, ErrorMessage = "El tipo de transacción debe ser 1 (Entrada) o 2 (Salida)")]
    [Column("tipotransaccion")]
    public int? TipoTransaccion { get; set; }     // changed to nullable

    [Display(Name = "Cantidad de Camiones")]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad de camiones debe ser mayor a 0")]
    [Column("cantidadcamiones")]
    public int? CantidadCamiones { get; set; }      // changed to nullable

    [Display(Name = "Cantidad Requerida")]
    [Range(0.01, double.MaxValue, ErrorMessage = "La cantidad requerida debe ser mayor a 0")]
    [Column("cantidadrequerida")]
    public double? CantidadRequerida { get; set; }  // changed to nullable

    [Display(Name = "Cantidad Entregada")]
    [Range(0.0, double.MaxValue, ErrorMessage = "La cantidad entregada debe ser mayor o igual a 0")]
    [Column("cantidadentregada")]
    public double? CantidadEntregada { get; set; }

    [Display(Name = "Placa")]
    [Column("placa")]
    public string? Placa { get; set; }

    [Display(Name = "Placa Alterna")]
    [Column("placa_alterna")]
    public string? PlacaAlterna { get; set; }

    //[Required(ErrorMessage = "La guía es requerida")]
    [Display(Name = "Guía")]
    [Column("guia")]
    public int? Guia { get; set; }

    [Display(Name = "Guía Alterna")]
    [Column("guia_alterna")]
    public string? GuiaAlterna { get; set; } = "N/A";

    //[Required(ErrorMessage = "La escotilla es requerida")]
    [Display(Name = "Escotilla")]
    [Range(1, 7, ErrorMessage = "La escotilla debe estar entre 1 y 7")]
    [Column("escotilla")]
    public int? Escotilla { get; set; }

    [Display(Name = "Total Carga")]
    [Range(0.0, double.MaxValue, ErrorMessage = "El total de carga debe ser mayor o igual a 0")]
    [Column("totalcarga")]
    public double? TotalCarga { get; set; }

    [Display(Name = "Bodega")]
    [Column("bodega")]
    public int? Bodega { get; set; }

    [Display(Name = "Usuario")]
    [Column("idusuario")]
    public int? IdUsuario { get; set; }

    // Constructor
    public Movimiento()
    {
        FechaHoraSystema = DateTime.Now;
        FechaHora = DateTime.Now;
        TipoTransaccion = 1;
        IdImportacion = 0;
        IdEmpresa = 0;
        CantidadCamiones = 0;
        CantidadRequerida = 0.0;
        CantidadEntregada = 0.0;
        Placa = null;
        PlacaAlterna = null;
        Guia = null;
        GuiaAlterna = "N/A";
        Escotilla = 1;
        TotalCarga = 0.0;
        Bodega = null;
    }
}