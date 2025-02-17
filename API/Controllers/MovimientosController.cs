using Microsoft.AspNetCore.Mvc;
using API.Models;
using API.Data;
using Microsoft.EntityFrameworkCore;


namespace API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class MovimientosController : ControllerBase
    {
        private readonly ApiContext _context;
        public MovimientosController(ApiContext context)
        {
            _context = context;
        }

        // Endpoint para crear un nuevo Movimiento
        [HttpPost]
        public JsonResult Create(Movimiento movimiento)
        {
            if (movimiento.id != 0)
            {
                return new JsonResult(BadRequest("El id debe ser 0 para crear un nuevo movimiento."));
            }
            _context.Movimientos.Add(movimiento);
            _context.SaveChanges();
            return new JsonResult(Ok(movimiento));
        }

        // Endpoint para editar un Movimiento existente
        [HttpPut]
        public JsonResult Edit(Movimiento movimiento)
        {
            if (movimiento.id == 0)
            {
                return new JsonResult(BadRequest("Debe proporcionar un id válido para editar un movimiento."));
            }
            var movimientoInDb = _context.Movimientos.Find(movimiento.id);
            if (movimientoInDb == null)
            {
                return new JsonResult(NotFound());
            }
            _context.Entry(movimientoInDb).CurrentValues.SetValues(movimiento);
            _context.SaveChanges();
            return new JsonResult(Ok(movimiento));
        }

        // Get
        [HttpGet]
        public JsonResult Get(int id)
        {
            var result = _context.Movimientos.Find(id);
            if (result == null)
            {
                return new JsonResult(NotFound());
            }
            return new JsonResult(Ok(result));
        }

        // Delete
        [HttpDelete]
        public JsonResult Delete(int id)
        {
            var result = _context.Movimientos.Find(id);
            if (result == null)
            {
                return new JsonResult(NotFound());
            }
            _context.Movimientos.Remove(result);
            _context.SaveChanges();
            return new JsonResult(NoContent());
        }

        // GetAll
        [HttpGet]
        public JsonResult GetAll()
        {
            var result = _context.Movimientos.ToList();
            return new JsonResult(Ok(result));
        }

        // Endpoint para obtener el informe general
        [HttpGet]
        public async Task<IActionResult> InformeGeneral(int? importacionId)
        {
            try
            {
                var importaciones = await _context.Importaciones
                    .Include(i => i.Barco)
                    .OrderByDescending(i => i.id)
                    .ToListAsync();

                var selectListItems = importaciones.Select(i => new
                {
                    Value = i.id.ToString(),
                    Text = $"#{i.id} - {(i.Barco?.nombrebarco ?? "Sin Barco")} - ({(i.fechahorasystema.HasValue ? i.fechahorasystema.Value.ToString("dd/MM/yyyy") : "Sin Fecha")})",
                    Selected = i.id == importacionId
                }).ToList();

                var informeData = await GetInformeData(importacionId);

                if (informeData == null || informeData.Count == 0)
                {
                    return NotFound(new { message = "No se encontraron registros para el informe." });
                }

                return Ok(new
                {
                    count = informeData.Count,
                    data = informeData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        private async Task<List<object>> GetInformeData(int? importacionId)
        {
            var analysis = await _context.Movimientos
                .Include(m => m.Empresa)
                .Where(m => !importacionId.HasValue || m.idimportacion == importacionId)
                .Select(m => new
                {
                    m.idempresa,
                    EmpresaNombre = m.Empresa != null ? m.Empresa.nombreempresa : "Sin Empresa",
                    m.idimportacion,
                    m.cantidadrequerida,
                    m.cantidadentregada,
                })
                .ToListAsync();

            // Agrupar por empresa
            var groupedByEmpresa = analysis
                .GroupBy(m => new { m.idempresa, m.EmpresaNombre })
                .Select(g => new
                {
                    g.Key.idempresa,
                    g.Key.EmpresaNombre,
                    ImportacionesCount = g.Select(x => x.idimportacion).Distinct().Count(),
                    MovimientosCount = g.Count(),
                    TotalRequerido = g.Sum(x => x.cantidadrequerida ?? 0),
                    TotalEntregado = g.Sum(x => x.cantidadentregada ?? 0)
                })
                .ToList();

            var informeData = groupedByEmpresa
                .Select(g => new
                {
                    Empresa = g.EmpresaNombre ?? "Sin Empresa",
                    RequeridoKg = g.TotalRequerido,
                    DescargaKg = g.TotalEntregado,
                    RequeridoTon = g.TotalRequerido / 1000,
                    FaltanteKg = g.TotalRequerido - g.TotalEntregado,
                    TonFaltantes = (g.TotalRequerido - g.TotalEntregado) / 1000,
                    CamionesFaltantes = (int)Math.Ceiling((g.TotalRequerido - g.TotalEntregado) / 30000.0m),
                    ConteoPlacas = _context.Movimientos
                        .Where(m => m.idempresa == g.idempresa && m.placa != null)
                        .Select(m => m.placa)
                        .Distinct()
                        .Count(),
                    PorcentajeDescarga = g.TotalRequerido > 0
                        ? (g.TotalEntregado / g.TotalRequerido) * 100
                        : 0
                })
                .OrderBy(x => x.Empresa)
                .ToList();

            return informeData.Cast<object>().ToList();
        }

        // Endpoint para obtener el registro de requerimientos sin usar RegistroRequerimientosViewModel
        [HttpGet]
        public async Task<IActionResult> RegistroRequerimientos([FromQuery] int? selectedBarco)
        {
            try
            {
                // Obtener la lista de barcos que tienen movimientos (tipotransaccion == 1)
                var barcos = await (from b in _context.Barcos
                                    join i in _context.Importaciones on b.id equals i.id
                                    join m in _context.Movimientos on i.id equals m.idimportacion
                                    where m.tipotransaccion == 1
                                    select b)
                                   .Distinct()
                                   .ToListAsync();

                // Si no se ha seleccionado un barco, se devuelve count 0, data vacía y la lista de barcos
                if (!selectedBarco.HasValue)
                {
                    return Ok(new
                    {
                        count = 0,
                        data = new List<object>(),
                        barcos
                    });
                }

                var result = await (from m in _context.Movimientos
                                    join i in _context.Importaciones on m.idimportacion equals i.id
                                    join e in _context.Empresas on m.idempresa equals e.id_empresa
                                    join b in _context.Barcos on i.idbarco equals b.id
                                    where b.id == selectedBarco.Value && m.tipotransaccion == 1
                                    select new
                                    {
                                        IdMovimiento = m.id,
                                        FechaHora = m.fechahora,
                                        IdImportacion = m.idimportacion,
                                        Importacion = b.nombrebarco ?? string.Empty,
                                        IdEmpresa = m.idempresa,
                                        Empresa = e.nombreempresa ?? string.Empty,
                                        TipoTransaccion = m.tipotransaccion ?? 0,
                                        CantidadRequerida = m.cantidadrequerida ?? 0,
                                        CantidadCamiones = m.cantidadcamiones ?? 0
                                    })
                                   .ToListAsync();

                return Ok(new
                {
                    count = result.Count,
                    data = result,
                    barcos
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }
    }
}

