using Microsoft.AspNetCore.Mvc;
using API.Models;
using API.Data;
using Microsoft.EntityFrameworkCore;
using Sistema_de_Gestion_de_Importaciones.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;

namespace API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class MovimientosController : ControllerBase
    {
        private readonly ApiContext _context;
        private readonly ILogger<MovimientosController> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly Dictionary<string, decimal> _conversionFactors = new Dictionary<string, decimal>();
        public MovimientosController(ApiContext context, ILogger<MovimientosController> logger, IMemoryCache memoryCache)
        {
            _context = context;
            _logger = logger;
            _memoryCache = memoryCache;
        }

        private async Task<Dictionary<string, decimal>> GetConversionFactors()
        {
            if (_conversionFactors.Count > 0)
                return _conversionFactors;

            try
            {
                var unidades = await _context.Unidades.ToListAsync();
                _conversionFactors.Clear();

                foreach (var unidad in unidades)
                {
                    if (unidad.nombre != null)
                    {
                        _conversionFactors[unidad.nombre.ToLower()] = unidad.valor;
                    }
                }

                if (!_conversionFactors.ContainsKey("quintales"))
                    _conversionFactors["quintales"] = 45.359237m;

                if (!_conversionFactors.ContainsKey("libras"))
                    _conversionFactors["libras"] = 2.20462m;

                if (!_conversionFactors.ContainsKey("toneladas"))
                    _conversionFactors["toneladas"] = 1000m;

                return _conversionFactors;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving conversion factors from database");

                return new Dictionary<string, decimal>
                {
                    ["quintales"] = 45.359237m,
                    ["libras"] = 2.20462m,
                    ["toneladas"] = 1000m
                };
            }
        }

        // Endpoint para crear un nuevo Movimiento
        [HttpPost]
        [Consumes("application/json")]
        public JsonResult Create([FromBody] Movimiento movimiento)
        {
            // First clear any model state errors related to fechahorasistema
            if (ModelState.ContainsKey("fechahorasistema"))
            {
                ModelState.Remove("fechahorasistema");
            }

            if (movimiento.id != 0)
            {
                return new JsonResult(BadRequest("El id debe ser 0 para crear un nuevo movimiento."));
            }

            // Always set the system timestamp for new records
            movimiento.fechahorasistema = DateTime.Now;

            // Ensure fechahora is set if not provided
            if (movimiento.fechahora == default)
            {
                movimiento.fechahora = DateTime.Now;
            }

            if (!ModelState.IsValid)
            {
                return new JsonResult(BadRequest(ModelState));
            }

            _context.Movimientos.Add(movimiento);
            _context.SaveChanges();
            return new JsonResult(Ok(movimiento));
        }

        // Endpoint para editar un Movimiento existente
        [HttpPut]
        public JsonResult Edit(Movimiento movimiento)
        {
            // First clear any model state errors related to fechahorasistema
            if (ModelState.ContainsKey("fechahorasistema"))
            {
                ModelState.Remove("fechahorasistema");
            }

            if (movimiento.id == 0)
            {
                return new JsonResult(BadRequest("Debe proporcionar un id válido para editar un movimiento."));
            }

            if (!ModelState.IsValid)
            {
                return new JsonResult(BadRequest(ModelState));
            }

            var movimientoInDb = _context.Movimientos.Find(movimiento.id);
            if (movimientoInDb == null)
            {
                return new JsonResult(NotFound());
            }

            // Preserve the original fechahorasistema
            movimiento.fechahorasistema = movimientoInDb.fechahorasistema;

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

        // Get All Movimientos por Importacion, de la vista de reporte individual detallado
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
                    .ToDictionary(e => e.id_empresa, e => e.nombreempresa ?? " - ");

                var bodegas = bodegasEntities
                    .Where(b => bodegaIds.Contains(b.id))
                    .ToDictionary(b => b.id, b => b.bodega ?? " - ");

                var factors = await GetConversionFactors();

                decimal KG_PER_QUINTAL = factors["quintales"];
                decimal KG_TO_LB = factors["libras"];
                decimal KG_TO_TON = 1m / factors["toneladas"];

                decimal cargaTotalInicial = (decimal)(importacion.totalcargakilos ?? 0);

                var empresaRequerimientos = await _context.Movimientos
                    .Where(m => m.idimportacion == importacionId && m.tipotransaccion == 1)
                    .GroupBy(m => m.idempresa)
                    .Select(g => new
                    {
                        EmpresaId = g.Key,
                        TotalRequerido = (decimal)g.Sum(m => m.cantidadrequerida ?? 0)
                    })
                    .ToDictionaryAsync(k => k.EmpresaId, v => v.TotalRequerido);

                var movimientosMapeados = new List<dynamic>();

                foreach (var m in movimientos)
                {
                    movimientosMapeados.Add(new
                    {
                        Id = m.id,
                        Escotilla = m.escotilla,
                        IdEmpresa = m.idempresa,
                        Empresa = empresas.ContainsKey(m.idempresa) ? empresas[m.idempresa] : " - ",
                        EmpresaNombre = empresas.ContainsKey(m.idempresa) ? empresas[m.idempresa] : " - ",
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

                var empresasProcesadas = new HashSet<int>();

                foreach (var mov in primerosRegistros.Concat(restantesRegistros))
                {
                    cantidadTotalAcumulada += mov.PesoEntregado;
                    decimal cantidadPorRetirar = cargaTotalInicial - cantidadTotalAcumulada;

                    bool esPrimerRegistro = !empresasProcesadas.Contains(mov.IdEmpresa);
                    decimal empresaRequerido = empresaRequerimientos.ContainsKey(mov.IdEmpresa)
                        ? empresaRequerimientos[mov.IdEmpresa]
                        : 0;

                    empresasProcesadas.Add(mov.IdEmpresa);

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

                        CantidadRequeridaQuintales = esPrimerRegistro
                            ? Math.Round(empresaRequerido / KG_PER_QUINTAL, 2)
                            : 0,

                        CantidadEntregadaQuintales = Math.Round(mov.PesoEntregado / KG_PER_QUINTAL, 10),

                        CantidadRequeridaLibras = esPrimerRegistro
                            ? Math.Round(empresaRequerido * KG_TO_LB, 2)
                            : 0,

                        CantidadEntregadaLibras = Math.Round(mov.PesoEntregado * KG_TO_LB, 4)
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

        // Endpoint para obtener el informe general, de la vista de informe general
        [HttpGet]
        public async Task<IActionResult> InformeGeneral(int? importacionId)
        {
            try
            {
                _logger.LogInformation($"Generando InformeGeneral para importacionId: {importacionId}");

                var factors = await GetConversionFactors();
                decimal KG_TO_TON = 1m / factors["toneladas"];

                var empresaTotalesQuery = await _context.Movimientos
                    .Where(m => !importacionId.HasValue || m.idimportacion == importacionId)
                    .GroupBy(m => m.idempresa)
                    .Select(g => new
                    {
                        EmpresaId = g.Key,
                        RequeridoKg = g.Where(m => m.tipotransaccion == 1)
                            .Sum(m => (decimal)(m.cantidadrequerida ?? 0)),
                        DescargaKg = g.Where(m => m.tipotransaccion == 2)
                            .Sum(m => (decimal)m.cantidadentregada),
                        CantidadEntregas = g.Where(m => m.tipotransaccion == 2 && m.cantidadentregada != 0)
                            .Count(),
                        SumaEntregas = g.Where(m => m.tipotransaccion == 2 && m.cantidadentregada != 0)
                            .Sum(m => (decimal)m.cantidadentregada),
                        TotalCamiones = g.Where(m => m.tipotransaccion == 1)
                            .Sum(m => m.cantidadcamiones ?? 0)
                    })
                    .ToListAsync();

                var empresasDict = await _context.Empresas
                    .ToDictionaryAsync(e => e.id_empresa, e => e.nombreempresa ?? "Sin Empresa");

                var totalMovimientos = await _context.Movimientos
                    .Where(m => m.idimportacion == importacionId && m.tipotransaccion == 2)
                    .CountAsync();

                var informeData = new List<InformeGeneralViewModel>(empresaTotalesQuery.Count);

                foreach (var item in empresaTotalesQuery)
                {
                    decimal requeridoKgDecimal = item.RequeridoKg;
                    decimal descargaKgDecimal = item.DescargaKg;
                    decimal faltanteKgDecimal = requeridoKgDecimal - descargaKgDecimal;
                    decimal faltanteTonDecimal = faltanteKgDecimal * KG_TO_TON;

                    decimal promedioEntregado = item.CantidadEntregas > 0
                        ? item.SumaEntregas / item.CantidadEntregas
                        : 0;

                    decimal promedioEntregadoTon = promedioEntregado * KG_TO_TON;

                    var informe = new InformeGeneralViewModel
                    {
                        EmpresaId = item.EmpresaId,
                        Empresa = empresasDict.GetValueOrDefault(item.EmpresaId, "Sin Empresa"),
                        RequeridoKg = (double)requeridoKgDecimal,
                        RequeridoTon = (double)(requeridoKgDecimal * KG_TO_TON),
                        DescargaKg = (double)descargaKgDecimal,
                        FaltanteKg = (double)faltanteKgDecimal,
                        TonFaltantes = (double)faltanteTonDecimal,
                        ConteoPlacas = item.TotalCamiones,
                        PorcentajeDescarga = requeridoKgDecimal > 0
                            ? (double)Math.Round((descargaKgDecimal / requeridoKgDecimal) * 100m, 2)
                            : 0
                    };

                    if (promedioEntregadoTon > 0)
                    {
                        decimal camionesFaltantesDecimal = faltanteTonDecimal / promedioEntregadoTon;
                        informe.CamionesFaltantes = Math.Round((double)camionesFaltantesDecimal, 2, MidpointRounding.AwayFromZero);
                    }
                    else
                    {
                        const decimal STANDARD_TRUCK_CAPACITY_KG = 30000m;
                        informe.CamionesFaltantes = Math.Round((double)(faltanteKgDecimal / STANDARD_TRUCK_CAPACITY_KG), 2);
                    }

                    informeData.Add(informe);
                }

                informeData = informeData.OrderBy(i => i.EmpresaId).ToList();

                var result = new
                {
                    data = informeData,
                    totalMovimientos = totalMovimientos
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en InformeGeneral: {Message}", ex.Message);
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // Endpoint para obtener el registro de requerimientos, de la vista de registro de requerimientos
        [HttpGet]
        public async Task<IActionResult> RegistroRequerimientos([FromQuery] int? selectedBarco)
        {
            try
            {
                var barcos = await (from b in _context.Barcos
                                    join i in _context.Importaciones on b.id equals i.id
                                    join m in _context.Movimientos on i.id equals m.idimportacion
                                    where m.tipotransaccion == 1
                                    select b)
                                   .Distinct()
                                   .ToListAsync();

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

        // Endpoint para obtener el cálculo de movimientos, segunda tabla de la vista de registro de pesajes de camiones con grana
        [HttpGet]
        public async Task<IActionResult> CalculoMovimientos([FromQuery] int importacionId, [FromQuery] int idempresa)
        {
            try
            {
                if (importacionId <= 0 || idempresa <= 0)
                {
                    return BadRequest(new { message = "ImportacionId y idempresa deben ser mayores a 0" });
                }

                var requerimientoTotal = await _context.Movimientos
                    .Where(m => m.idimportacion == importacionId &&
                               m.idempresa == idempresa &&
                               m.tipotransaccion == 1)
                    .Select(m => m.cantidadrequerida ?? 0)
                    .FirstOrDefaultAsync();

                var movimientosTipo1 = await _context.Movimientos
                    .Where(m => m.idimportacion == importacionId &&
                               m.idempresa == idempresa &&
                               m.tipotransaccion == 1)
                    .OrderBy(m => m.id)
                    .ThenBy(m => m.guia)
                    .Select(m => new
                    {
                        m.id,
                        m.escotilla,
                        m.bodega,
                        m.guia,
                        m.guia_alterna,
                        m.placa,
                        m.placa_alterna,
                        m.cantidadrequerida,
                        m.tipotransaccion,
                        m.fechahora
                    })
                    .ToListAsync();

                var movimientosTipo2 = await _context.Movimientos
                    .Where(m => m.idimportacion == importacionId &&
                               m.idempresa == idempresa &&
                               m.tipotransaccion == 2)
                    .OrderBy(m => m.id)
                    .ThenBy(m => m.guia).
                    Select(m => new
                    {
                        m.id,
                        m.bodega,
                        m.guia,
                        m.guia_alterna,
                        m.placa,
                        m.placa_alterna,
                        m.cantidadentregada,
                        m.tipotransaccion,
                        m.fechahora,
                        m.escotilla
                    })
                    .ToListAsync();

                var result = new List<MovimientosCumulatedDto>();
                decimal acumulado = 0;

                foreach (var mov in movimientosTipo1)
                {
                    var matchingDelivery = movimientosTipo2
                        .FirstOrDefault(m => m.guia == mov.guia ||
                                        (!string.IsNullOrEmpty(m.placa) && m.placa == mov.placa));

                    decimal entregado = matchingDelivery?.cantidadentregada ?? 0;

                    result.Add(new MovimientosCumulatedDto
                    {
                        id = mov.id,
                        escotilla = mov.escotilla ?? 0,
                        bodega = mov.bodega?.ToString() ?? "",
                        guia = mov.guia?.ToString() ?? "",
                        guia_alterna = mov.guia_alterna ?? "",
                        placa = mov.placa ?? "",
                        placa_alterna = mov.placa_alterna ?? "",
                        cantidadrequerida = (decimal)(mov.cantidadrequerida ?? 0),
                        cantidadentregada = entregado, // Use matching delivery if found
                        peso_faltante = (decimal)(mov.cantidadrequerida ?? 0) - entregado,
                        porcentaje = mov.cantidadrequerida > 0
                            ? Math.Round((entregado * 100 / (decimal)(mov.cantidadrequerida ?? 0)), 2)
                            : 0,
                    });
                }

                foreach (var mov in movimientosTipo2)
                {
                    acumulado += mov.cantidadentregada;

                    result.Add(new MovimientosCumulatedDto
                    {
                        id = mov.id,
                        escotilla = mov.escotilla ?? 0,
                        bodega = mov.bodega?.ToString() ?? "",
                        guia = mov.guia?.ToString() ?? "",
                        guia_alterna = mov.guia_alterna ?? "",
                        placa = mov.placa ?? "",
                        placa_alterna = mov.placa_alterna ?? "",
                        cantidadrequerida = 0,
                        cantidadentregada = mov.cantidadentregada,
                        peso_faltante = requerimientoTotal - acumulado,
                        porcentaje = requerimientoTotal > 0
                            ? Math.Round((acumulado * 100 / requerimientoTotal), 2)
                            : 0,
                    });
                }

                if (!result.Any())
                {
                    return Ok(new
                    {
                        count = 0,
                        data = new List<MovimientosCumulatedDto>(),
                        requeridoTotal = requerimientoTotal,
                        message = "No hay movimientos registrados"
                    });
                }

                int countPesajes = movimientosTipo2.Count;

                return Ok(new
                {
                    count = countPesajes,
                    data = result,
                    requeridoTotal = requerimientoTotal,
                    message = requerimientoTotal == 0
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

        // Endpoint para obtener el cálculo de escotillas por empresa, equivalente a la tabla del barco en el sistema de importaciones
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

                // Calcular el total de kilos requeridos para esta importación
                var totalKilosRequeridos = await _context.Movimientos
                    .Where(m => m.idimportacion == importacionId && m.tipotransaccion == 1)
                    .SumAsync(m => (decimal?)(m.cantidadrequerida ?? 0)) ?? 0;

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
                        EstadoGeneral = totalDiferencia > 0 ? "Faltante" : "Sobrante",
                        TotalKilosRequeridos = totalKilosRequeridos
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

        //Endpoint para el reporte general de descargas, agrupado por empresa, con sus conversiones en kilos, libras, toneladas y quintales.
        [HttpGet]
        public async Task<IActionResult> CalculoEscotillasPorEmpresa([FromQuery] int importacionId)
        {
            try
            {
                if (importacionId <= 0)
                {
                    return BadRequest(new { message = "ImportacionId debe ser mayor a 0" });
                }

                var factors = await GetConversionFactors();

                decimal KG_PER_QUINTAL = factors["quintales"];
                decimal KG_TO_LB = factors["libras"];
                decimal KG_TO_TON = 1m / factors["toneladas"];

                var importacion = await _context.Importaciones
                    .Include(i => i.Barco)
                    .FirstOrDefaultAsync(i => i.id == importacionId);

                if (importacion?.Barco == null)
                {
                    return NotFound(new { message = "No se encontró la importación o el barco asociado" });
                }

                var capacidadesPorEscotilla = importacion.Barco.ObtenerCapacidadesEscotillas();

                var movimientosPorEmpresaEscotilla = await (from m in _context.Movimientos
                                                            join e in _context.Empresas on m.idempresa equals e.id_empresa
                                                            where m.idimportacion == importacionId
                                                            && m.tipotransaccion == 2
                                                            && m.escotilla != null
                                                            group m by new { m.idempresa, e.nombreempresa, m.escotilla } into g
                                                            select new
                                                            {
                                                                IdEmpresa = g.Key.idempresa,
                                                                NombreEmpresa = g.Key.nombreempresa ?? "Sin Nombre",
                                                                Escotilla = g.Key.escotilla,
                                                                DescargaReal = g.Sum(x => x.cantidadentregada)
                                                            }).ToListAsync();

                // Agrupar por empresa
                var empresasAgrupadoEscotillas = movimientosPorEmpresaEscotilla
                    .GroupBy(m => new { m.IdEmpresa, m.NombreEmpresa })
                    .Select(g => new
                    {
                        IdEmpresa = g.Key.IdEmpresa,
                        NombreEmpresa = g.Key.NombreEmpresa,
                        Escotillas = g.Select(m => new
                        {
                            NumeroEscotilla = m.Escotilla,
                            DescargaKg = Math.Round(m.DescargaReal, 2),
                            DescargaQuintales = Math.Round(m.DescargaReal / KG_PER_QUINTAL, 4),
                            DescargaLb = Math.Round(m.DescargaReal * KG_TO_LB, 0),
                            DescargaTon = Math.Round(m.DescargaReal * KG_TO_TON, 6),
                        }).OrderBy(e => e.NumeroEscotilla).ToList()
                    })
                    .OrderBy(e => e.NombreEmpresa)
                    .ToList();

                return Ok(new
                {
                    empresas = empresasAgrupadoEscotillas
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error al procesar la solicitud",
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
    }
}

