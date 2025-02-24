using Microsoft.AspNetCore.Mvc;
using API.Models;
using API.Data;
using Microsoft.EntityFrameworkCore;
using Sistema_de_Gestion_de_Importaciones.Models.ViewModels;


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
        [Consumes("application/json")]
        public JsonResult Create([FromBody] Movimiento movimiento)
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
                var query = from m in _context.Movimientos
                            join i in _context.Importaciones on m.idimportacion equals i.id
                            join e in _context.Empresas on m.idempresa equals e.id_empresa
                            join b in _context.Barcos on i.idbarco equals b.id
                            where (!importacionId.HasValue || i.id == importacionId)
                            group m by new { e.id_empresa, e.nombreempresa } into g
                            select new InformeGeneralViewModel
                            {
                                Empresa = g.Key.nombreempresa ?? "Sin Empresa",
                                RequeridoKg = (double)g.Where(m => m.tipotransaccion == 1)
                                             .Sum(m => m.cantidadrequerida ?? 0),
                                DescargaKg = (double)g.Where(m => m.tipotransaccion == 2)
                                            .Sum(m => m.cantidadentregada ?? 0),
                                ConteoPlacas = g.Where(m => m.placa != null)
                                              .Select(m => m.placa)
                                              .Distinct()
                                              .Count()
                            };

                var informeData = await query.ToListAsync();

                // Calculate derived fields
                foreach (var item in informeData)
                {
                    item.RequeridoTon = item.RequeridoKg / 1000;
                    item.FaltanteKg = item.RequeridoKg - item.DescargaKg;
                    item.TonFaltantes = item.FaltanteKg / 1000;
                    // Reemplazamos: ya no se redondea hacia arriba y se deja a 2 decimales, permitiendo negativos
                    item.CamionesFaltantes = Math.Round(item.FaltanteKg / 30000.0, 2);
                    item.PorcentajeDescarga = item.RequeridoKg > 0
                        ? (item.DescargaKg / item.RequeridoKg) * 100
                        : 0;
                }

                return Ok(new { data = informeData });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
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
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        // Endpoint que ejecuta el query acumulativo
        [HttpGet]
        public async Task<IActionResult> CalculoMovimientos([FromQuery] int importacionId, [FromQuery] int idempresa)
        {
            try
            {
                // Obtener y ordenar los movimientos directamente en la consulta
                var movimientosList = await _context.Movimientos
                    .Where(m => m.idimportacion == importacionId && m.idempresa == idempresa)
                    .ToListAsync();

                // Ordenar la lista usando ordenamiento numérico para las guías
                movimientosList = movimientosList
                    .OrderBy(m =>
                    {
                        if (string.IsNullOrEmpty(m.guia?.ToString())) return int.MinValue;
                        return int.TryParse(m.guia.ToString(), out int numero) ? numero : int.MinValue;
                    })
                    .ToList();

                if (!movimientosList.Any())
                {
                    return Ok(new { count = 0, data = new List<MovimientosCumulatedDto>() });
                }

                // Resto del código igual...
                decimal initialRequired = movimientosList.First().cantidadrequerida ?? 0;

                if (initialRequired <= 0)
                {
                    return BadRequest(new { message = "El primer movimiento no tiene una cantidad requerida válida." });
                }

                decimal cumulativeDelivered = 0;
                var result = new List<MovimientosCumulatedDto>();

                foreach (var m in movimientosList)
                {
                    decimal delivered = m.cantidadentregada ?? 0;
                    cumulativeDelivered += delivered;
                    // Peso faltante: valor inicial menos lo acumulado
                    decimal pesoFaltante = initialRequired - cumulativeDelivered;
                    // Porcentaje de entrega
                    decimal porcentaje = initialRequired > 0 ? (cumulativeDelivered / initialRequired) * 100 : 0;

                    result.Add(new MovimientosCumulatedDto
                    {
                        bodega = m.bodega?.ToString() ?? string.Empty,
                        guia = m.guia?.ToString() ?? string.Empty,
                        placa = m.placa ?? string.Empty,
                        cantidadrequerida = m.cantidadrequerida ?? 0,
                        cantidadentregada = delivered,
                        peso_faltante = pesoFaltante,
                        porcentaje = Math.Round(porcentaje, 2)
                    });
                }

                return Ok(new { count = result.Count, data = result });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, new { message = $"Error al calcular los movimientos: {ex.Message}" });
            }
        }
    }
}

