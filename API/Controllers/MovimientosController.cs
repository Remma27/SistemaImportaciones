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
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var result = await _context.Movimientos
                    .AsNoTracking()
                    .OrderByDescending(m => m.id)
                    .ToListAsync();

                return Ok(new
                {
                    count = result.Count(),
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener movimientos", error = ex.Message });
            }
        }

        // Get All Movimientos por Importacion
        [HttpGet]
        public async Task<IActionResult> GetAllByImportacion(int importacionId)
        {
            try
            {
                var importacion = await _context.Importaciones
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.id == importacionId);

                if (importacion == null)
                {
                    return NotFound(new { message = "Importación no encontrada" });
                }

                var movimientos = await _context.Movimientos
                    .Where(m => m.idimportacion == importacionId)
                    .AsNoTracking()
                    .ToListAsync();

                if (!movimientos.Any())
                {
                    return Ok(new
                    {
                        count = 0,
                        data = new List<object>()
                    });
                }

                var empresaIds = movimientos.Select(m => m.idempresa).Distinct().ToList();
                var bodegaIds = movimientos.Where(m => m.bodega.HasValue)
                                          .Select(m => m.bodega!.Value)
                                          .Distinct()
                                          .ToList();

                var empresasEntities = await _context.Empresas.AsNoTracking().ToListAsync();
                var bodegasEntities = await _context.Empresa_Bodegas.AsNoTracking().ToListAsync();

                var empresas = empresasEntities
                    .Where(e => empresaIds.Contains(e.id_empresa))
                    .ToDictionary(e => e.id_empresa, e => e.nombreempresa ?? "Sin Empresa");

                var bodegas = bodegasEntities
                    .Where(b => bodegaIds.Contains(b.id))
                    .ToDictionary(b => b.id, b => b.bodega ?? "Sin Bodega");

                const decimal KG_TO_LB = 2.20462m;
                const decimal KG_PER_QUINTAL = 45.359237m;

                decimal cargaTotalInicial = (decimal)(importacion.totalcargakilos ?? 0);

                var movimientosMapeados = new List<dynamic>();

                foreach (var m in movimientos)
                {
                    movimientosMapeados.Add(new
                    {
                        Id = m.id,
                        Escotilla = m.escotilla,
                        IdEmpresa = m.idempresa,
                        Empresa = empresas.ContainsKey(m.idempresa) ? empresas[m.idempresa] : "Sin Empresa",
                        EmpresaNombre = empresas.ContainsKey(m.idempresa) ? empresas[m.idempresa] : "Sin Empresa",
                        Bodega = m.bodega.HasValue && bodegas.ContainsKey(m.bodega.Value) ? bodegas[m.bodega.Value] : "Sin Bodega",
                        Guia = m.guia,
                        GuiaAlterna = m.guia_alterna,
                        Placa = m.placa,
                        PlacaAlterna = m.placa_alterna,
                        PesoEntregado = m.cantidadentregada,
                        PesoEntregadoLibras = m.cantidadentregada * KG_TO_LB,
                        CantidadRequerida = m.cantidadrequerida ?? 0
                    });
                }

                var primerosRegistros = movimientosMapeados
                    .GroupBy(m => m.IdEmpresa)
                    .Select(group => group.OrderBy(m => m.Id).First())
                    .OrderBy(m => m.EmpresaNombre)
                    .ToList();

                var restantesRegistros = movimientosMapeados
                    .Where(m => !primerosRegistros.Any(p => p.Id == m.Id))
                    .OrderBy(m => m.EmpresaNombre)
                    .ThenBy(m => m.Id)
                    .ToList();

                var movimientosFinales = new List<object>();
                decimal cantidadTotalAcumulada = 0;

                foreach (var mov in primerosRegistros.Concat(restantesRegistros))
                {
                    cantidadTotalAcumulada += mov.PesoEntregado;

                    decimal cantidadPorRetirar = cargaTotalInicial - cantidadTotalAcumulada;

                    movimientosFinales.Add(new
                    {
                        Escotilla = mov.Escotilla,
                        EmpresaNombre = mov.EmpresaNombre,
                        Bodega = mov.Bodega,
                        Guia = mov.Guia,
                        GuiaAlterna = mov.GuiaAlterna,
                        Placa = mov.Placa,
                        PlacaAlterna = mov.PlacaAlterna,
                        PesoEntregado = Math.Round(mov.PesoEntregado, 2),
                        CantidadRetirarKg = Math.Round(cantidadPorRetirar, 2),
                        CantidadRetiradaKg = Math.Round(cantidadTotalAcumulada, 2),
                        CantidadRequeridaQuintales = Math.Round(cantidadPorRetirar / KG_PER_QUINTAL, 2) == 0 ? 0 : Math.Round(cantidadPorRetirar / KG_PER_QUINTAL, 2),
                        CantidadEntregadaQuintales = Math.Round(cantidadTotalAcumulada / KG_PER_QUINTAL, 10) == 0 ? 0 : Math.Round(cantidadTotalAcumulada / KG_PER_QUINTAL, 10),

                        CantidadRequeridaLibras = Math.Round(cantidadPorRetirar * KG_TO_LB, 2),
                        CantidadEntregadaLibras = Math.Round(cantidadTotalAcumulada * KG_TO_LB, 3)
                    });
                }

                return Ok(new
                {
                    count = movimientosFinales.Count,
                    data = movimientosFinales
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error al obtener movimientos",
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
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
                                            .Sum(m => m.cantidadentregada),
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
                                    where i.id == selectedBarco.Value && m.tipotransaccion == 1
                                    select new
                                    {
                                        IdMovimiento = m.id,
                                        FechaHora = m.fechahora,
                                        IdImportacion = m.idimportacion,
                                        Importacion = b.nombrebarco ?? string.Empty,
                                        IdEmpresa = m.idempresa,
                                        Empresa = e.nombreempresa ?? string.Empty,
                                        TipoTransaccion = m.tipotransaccion,
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
                if (importacionId <= 0 || idempresa <= 0)
                {
                    return BadRequest(new { message = "ImportacionId y idempresa deben ser mayores a 0" });
                }

                // Combine both queries into a single database call
                var data = await (from m in _context.Movimientos
                                  where m.idimportacion == importacionId &&
                                        m.idempresa == idempresa
                                  group m by m.tipotransaccion into g
                                  select new
                                  {
                                      TipoTransaccion = g.Key,
                                      Total = g.Key == 1
                                          ? g.Sum(x => x.cantidadrequerida ?? 0)
                                          : 0,
                                      Movimientos = g.Key == 2
                                          ? g.OrderBy(x => x.fechahora)
                                            .Select(x => new
                                            {
                                                x.id,
                                                x.bodega,
                                                x.guia,
                                                x.guia_alterna,
                                                x.placa,
                                                x.placa_alterna,
                                                x.cantidadentregada
                                            })
                                          : null
                                  }).ToListAsync();

                var requerimiento = data.FirstOrDefault(x => x.TipoTransaccion == 1)?.Total ?? 0;
                var movimientos = data.FirstOrDefault(x => x.TipoTransaccion == 2)?.Movimientos?.ToList();

                if (movimientos == null || !movimientos.Any())
                {
                    return Ok(new
                    {
                        count = 0,
                        data = new List<MovimientosCumulatedDto>(),
                        message = "No hay movimientos registrados para esta combinación"
                    });
                }

                // Process results in memory with optimized list capacity
                var result = new List<MovimientosCumulatedDto>(movimientos.Count);
                decimal acumulado = 0;

                foreach (var mov in movimientos)
                {
                    acumulado += mov.cantidadentregada;

                    result.Add(new MovimientosCumulatedDto
                    {
                        id = mov.id,
                        bodega = mov.bodega?.ToString() ?? "",
                        guia = mov.guia?.ToString() ?? "",
                        guia_alterna = mov.guia_alterna ?? "",
                        placa = mov.placa ?? "",
                        placa_alterna = mov.placa_alterna ?? "",
                        cantidadrequerida = requerimiento,
                        cantidadentregada = mov.cantidadentregada,
                        peso_faltante = requerimiento - acumulado,
                        porcentaje = requerimiento > 0
                            ? Math.Round((acumulado * 100 / requerimiento), 2)
                            : 0
                    });
                }

                return Ok(new
                {
                    count = result.Count,
                    data = result,
                    requerimiento = requerimiento > 0,
                    requeridoTotal = requerimiento,
                    message = requerimiento == 0
                        ? "No se encontró un requerimiento inicial"
                        : null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error al procesar la solicitud",
                    error = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CalculoEscotillas([FromQuery] int importacionId)
        {
            try
            {
                if (importacionId <= 0)
                {
                    return BadRequest(new { message = "ImportacionId debe ser mayor a 0" });
                }

                // Primero obtenemos el barco y sus capacidades por escotilla
                var importacion = await _context.Importaciones
                    .Include(i => i.Barco)
                    .FirstOrDefaultAsync(i => i.id == importacionId);

                if (importacion?.Barco == null)
                {
                    return NotFound(new { message = "No se encontró la importación o el barco asociado" });
                }

                // Obtener los movimientos agrupados por escotilla
                var movimientosPorEscotilla = await (from m in _context.Movimientos
                                                     where m.idimportacion == importacionId
                                                     && m.tipotransaccion == 2 // Solo movimientos de descarga
                                                     && m.escotilla != null
                                                     group m by m.escotilla into g
                                                     select new
                                                     {
                                                         Escotilla = g.Key,
                                                         DescargaReal = g.Sum(x => x.cantidadentregada)
                                                     }).ToListAsync();

                // Procesar las capacidades del barco y calcular diferencias
                var result = new List<object>();
                var capacidadesPorEscotilla = importacion.Barco.ObtenerCapacidadesEscotillas();

                foreach (var capacidad in capacidadesPorEscotilla)
                {
                    var descarga = movimientosPorEscotilla
                        .FirstOrDefault(m => m.Escotilla == capacidad.Key)?.DescargaReal ?? 0;

                    var diferencia = capacidad.Value - descarga;
                    var porcentaje = capacidad.Value > 0
                        ? Math.Round((descarga * 100.0M / capacidad.Value), 2)
                        : 0;

                    result.Add(new
                    {
                        NumeroEscotilla = capacidad.Key,
                        CapacidadKg = capacidad.Value,
                        DescargaRealKg = descarga,
                        DiferenciaKg = diferencia,
                        Porcentaje = porcentaje > 100 ? porcentaje - 100 : porcentaje,
                        Estado = diferencia > 0 ? "Faltante" : "Sobrante"
                    });
                }

                // Calcular totales
                var totalCapacidad = capacidadesPorEscotilla.Sum(x => x.Value);
                var totalDescarga = movimientosPorEscotilla.Sum(x => x.DescargaReal);
                var totalDiferencia = totalCapacidad - totalDescarga;
                var porcentajeTotal = totalCapacidad > 0
                    ? Math.Round((totalDescarga * 100.0M / totalCapacidad), 2)
                    : 0;

                return Ok(new
                {
                    escotillas = result,
                    totales = new
                    {
                        CapacidadTotal = totalCapacidad,
                        DescargaTotal = totalDescarga,
                        DiferenciaTotal = totalDiferencia,
                        PorcentajeTotal = porcentajeTotal > 100 ? porcentajeTotal - 100 : porcentajeTotal,
                        EstadoGeneral = totalDiferencia > 0 ? "Faltante" : "Sobrante"
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error al procesar la solicitud",
                    error = ex.Message
                });
            }
        }
    }
}

