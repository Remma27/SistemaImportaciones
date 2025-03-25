using Microsoft.AspNetCore.Mvc;
using API.Models;
using API.Data;
using Microsoft.EntityFrameworkCore;
using Sistema_de_Gestion_de_Importaciones.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using API.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        private readonly HistorialService _historialService;
        private readonly Dictionary<string, decimal> _conversionFactors = new Dictionary<string, decimal>();

        public MovimientosController(ApiContext context, ILogger<MovimientosController> logger, IMemoryCache memoryCache, HistorialService historialService)
        {
            _context = context;
            _logger = logger;
            _memoryCache = memoryCache;
            _historialService = historialService;
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

        private DateTime GetCostaRicaTime()
        {
            try
            {
                var costaRicaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, costaRicaTimeZone);
            }
            catch (Exception)
            {
                return DateTime.UtcNow.AddHours(-6);
            }
        }

        [HttpPost]
        [Consumes("application/json")]
        [Authorize(Roles = "Administrador,Operador")]
        public async Task<IActionResult> Create([FromBody] Movimiento movimiento)
        {
            if (ModelState.ContainsKey("fechahorasistema"))
            {
                ModelState.Remove("fechahorasistema");
            }

            try 
            {
                if (movimiento.id != 0)
                {
                    return BadRequest("El id debe ser 0 para crear un nuevo movimiento.");
                }

                movimiento.fechahorasistema = GetCostaRicaTime();

                if (movimiento.fechahora == default)
                {
                    movimiento.fechahora = GetCostaRicaTime();
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _context.Movimientos.Add(movimiento);
                await _context.SaveChangesAsync();
                
                _historialService.GuardarHistorial("CREAR", movimiento, "Movimientos", $"Creación de movimiento {movimiento.id}");
                
                return Ok(movimiento);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear movimiento");
                return StatusCode(500, new { message = "Error al crear movimiento", error = ex.Message });
            }
        }

        [HttpPut]
        [Authorize(Roles = "Administrador,Operador")]
        public async Task<IActionResult> Edit(Movimiento movimiento)
        {
            if (ModelState.ContainsKey("fechahorasistema"))
            {
                ModelState.Remove("fechahorasistema");
            }

            try
            {
                if (movimiento.id == 0)
                {
                    return BadRequest("Debe proporcionar un id válido para editar un movimiento.");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var movimientoInDb = await _context.Movimientos.FindAsync(movimiento.id);
                if (movimientoInDb == null)
                {
                    return NotFound();
                }

                _historialService.GuardarHistorial(
                    "ANTES_EDITAR", 
                    movimientoInDb, 
                    "Movimientos", 
                    $"Estado anterior de movimiento ID: {movimientoInDb.id}"
                );
                
                movimiento.fechahorasistema = movimientoInDb.fechahorasistema;

                _context.Entry(movimientoInDb).CurrentValues.SetValues(movimiento);
                await _context.SaveChangesAsync();
                
                _historialService.GuardarHistorial(
                    "DESPUES_EDITAR", 
                    movimiento, 
                    "Movimientos", 
                    $"Estado nuevo de movimiento ID: {movimiento.id}"
                );
                
                return Ok(movimiento);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar movimiento: {Id}", movimiento.id);
                return StatusCode(500, new { message = "Error al editar movimiento", error = ex.Message });
            }
        }

        // Get
        [HttpGet]
        [Authorize(Roles = "Administrador,Operador,Reporteria")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var result = await _context.Movimientos.FindAsync(id);
                if (result == null)
                {
                    return NotFound();
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener movimiento: {Id}", id);
                return StatusCode(500, new { message = "Error al obtener movimiento", error = ex.Message });
            }
        }

        // Delete
        [HttpDelete]
        [Authorize(Roles = "Administrador, Operador")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _context.Movimientos.FindAsync(id);
                if (result == null)
                {
                    return NotFound();
                }
                
                // Check if this is a requirement (tipotransaccion=1) and has associated weight records
                if (result.tipotransaccion == 1)
                {
                    var hasWeightRecords = await _context.Movimientos.AnyAsync(m => 
                        m.idimportacion == result.idimportacion && 
                        m.idempresa == result.idempresa && 
                        m.tipotransaccion == 2);
                        
                    if (hasWeightRecords)
                    {
                        return BadRequest(new { 
                            message = "No se puede eliminar el requerimiento porque ya tiene pesajes registrados." 
                        });
                    }
                }
                
                _historialService.GuardarHistorial("ELIMINAR", result, "Movimientos", $"Eliminación de movimiento {result.id}");
                
                _context.Movimientos.Remove(result);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar movimiento: {Id}", id);
                return StatusCode(500, new { message = "Error al eliminar movimiento", error = ex.Message });
            }
        }

        // GetAll
        [HttpGet]
        [Authorize(Roles = "Administrador,Operador")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var result = await _context.Movimientos
                    .AsNoTracking()
                    .OrderByDescending(m => m.id)
                    .ToListAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener movimientos");
                return StatusCode(500, new { message = "Error al obtener movimientos", error = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Administrador,Operador,Reporteria")]
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

        [HttpGet]
        [Authorize(Roles = "Administrador,Operador,Reporteria")]
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

        [HttpGet]
        [Authorize(Roles = "Administrador,Operador,Reporteria")]
        public async Task<IActionResult> RegistroRequerimientos([FromQuery] int? selectedBarco)
        {
            try
            {
                var barcos = await (from b in _context.Barcos
                                    join i in _context.Importaciones on b.id equals i.idbarco 
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
                        barcos = barcos.Select(b => new { b.id, b.nombrebarco }).ToList()
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

                var serializerOptions = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.IgnoreCycles,
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var response = new
                {
                    count = result.Count,
                    data = result, 
                    barcos = barcos.Select(b => new { b.id, b.nombrebarco }).ToList()
                };

                return new JsonResult(response, serializerOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RegistroRequerimientos");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Administrador,Operador,Reporteria")]
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
                        cantidadentregada = entregado, 
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
        [Authorize(Roles = "Administrador,Operador,Reporteria")]
        public async Task<IActionResult> CalculoEscotillas([FromQuery] int importacionId)
        {
            try
            {
                if (importacionId <= 0)
                {
                    return BadRequest(new { message = "ImportacionId debe ser mayor a 0" });
                }

                var importacion = await _context.Importaciones
                    .Include(i => i.Barco)
                    .FirstOrDefaultAsync(i => i.id == importacionId);

                if (importacion?.Barco == null)
                {
                    return NotFound(new { message = "No se encontró la importación o el barco asociado" });
                }

                var movimientosPorEscotilla = await (from m in _context.Movimientos
                                                     where m.idimportacion == importacionId
                                                     && m.tipotransaccion == 2 
                                                     && m.escotilla != null
                                                     group m by m.escotilla into g
                                                     select new
                                                     {
                                                         Escotilla = g.Key,
                                                         DescargaReal = g.Sum(x => x.cantidadentregada)
                                                     }).ToListAsync();

                var totalKilosRequeridos = await _context.Movimientos
                    .Where(m => m.idimportacion == importacionId && m.tipotransaccion == 1)
                    .SumAsync(m => (decimal?)(m.cantidadrequerida ?? 0)) ?? 0;

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
        [Authorize(Roles = "Administrador,Operador,Reporteria")]
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

