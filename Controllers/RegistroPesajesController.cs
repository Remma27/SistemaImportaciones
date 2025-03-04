using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sistema_de_Gestion_de_Importaciones.ViewModels;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using API.Models;
using Sistema_de_Gestion_de_Importaciones.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;

namespace Sistema_de_Gestion_de_Importaciones.Controllers
{
    [Authorize]
    public class RegistroPesajesController : Controller
    {
        private readonly IMovimientoService _movimientoService;
        private readonly IBodegaService _bodegaService;
        private readonly ILogger<RegistroPesajesController> _logger;
        private readonly IImportacionService _importacionService;
        private readonly IMemoryCache _memoryCache;
        private const int BARCOS_CACHE_DURATION_HOURS = 1;
        private const int BODEGAS_CACHE_DURATION_HOURS = 1;
        private const int REPORTE_CACHE_DURATION_MINUTES = 30;
        private const int DATA_CACHE_DURATION_MINUTES = 5;

        public RegistroPesajesController(
            IMovimientoService movimientoService,
            IBodegaService bodegaService,
            ILogger<RegistroPesajesController> logger,
            IImportacionService importacionService,
            IMemoryCache memoryCache)
        {
            _movimientoService = movimientoService;
            _bodegaService = bodegaService;
            _logger = logger;
            _importacionService = importacionService;
            _memoryCache = memoryCache;
        }

        public async Task<IActionResult> Index(int? selectedBarco, int? empresaId)
        {
            var watch = Stopwatch.StartNew();
            try
            {
                string cacheKey = $"RegistroPesajes_{selectedBarco}_{empresaId}";

                var viewModel = new RegistroPesajesViewModel
                {
                    Tabla1Data = new List<RegistroPesajesIndividual>(),
                    Tabla2Data = new List<RegistroPesajesAgregado>(),
                    TotalesPorBodega = new List<TotalesPorBodegaViewModel>(),
                    EscotillasData = new List<EscotillaViewModel>()
                };

                await PopulateDropdowns(selectedBarco, empresaId);

                if (!selectedBarco.HasValue)
                {
                    return View(viewModel);
                }

                if (_memoryCache.TryGetValue(cacheKey, out RegistroPesajesViewModel? cachedViewModel) && cachedViewModel != null)
                {
                    _logger.LogDebug($"Datos recuperados desde caché para barco {selectedBarco}, empresa {empresaId}");
                    return View(cachedViewModel);
                }

                int importacionId = selectedBarco.Value;

                if (empresaId.HasValue)
                {
                    var calculo = await _movimientoService.CalculoMovimientos(importacionId, empresaId.Value);

                    if (calculo != null && calculo.Any())
                    {
                        viewModel.Tabla1Data = calculo
                            .Where(c => c != null)
                            .Select(c => new RegistroPesajesIndividual
                            {
                                Id = c.id,
                                IdImportacion = c.idimportacion,
                                IdEmpresa = c.idempresa,
                                Guia = c.guia?.ToString() ?? string.Empty,
                                GuiaAlterna = c.guia_alterna,
                                Placa = c.placa,
                                PlacaAlterna = c.placa_alterna,
                                PesoRequerido = c.cantidadrequerida ?? 0,
                                PesoEntregado = c.cantidadentregada,
                                PesoFaltante = c.peso_faltante,
                                Porcentaje = c.porcentaje,
                                Escotilla = c.escotilla ?? 0,
                                Bodega = c.bodega?.ToString() ?? string.Empty,
                            }).ToList();

                        var bodegasDict = await GetBodegasDictionaryAsync();
                        viewModel.TotalesPorBodega = CalculateTotalesPorBodega(calculo, bodegasDict);
                    }

                    var informeGeneralTask = _movimientoService.GetInformeGeneralAsync(importacionId);
                    var escotillasTask = _movimientoService.GetEscotillasDataAsync(importacionId);

                    await Task.WhenAll(informeGeneralTask, escotillasTask);

                    var informeGeneral = await informeGeneralTask;
                    if (informeGeneral != null)
                    {
                        viewModel.Tabla2Data = informeGeneral.Select(ig => new RegistroPesajesAgregado
                        {
                            Agroindustria = ig.Empresa ?? "Sin nombre",
                            KilosRequeridos = (decimal)ig.RequeridoKg,
                            ToneladasRequeridas = (decimal)ig.RequeridoTon,
                            DescargaKilos = (decimal)ig.DescargaKg,
                            FaltanteKilos = (decimal)ig.FaltanteKg,
                            ToneladasFaltantes = (decimal)ig.TonFaltantes,
                            CamionesFaltantes = (decimal)ig.CamionesFaltantes,
                            ConteoPlacas = ig.ConteoPlacas,
                            PorcentajeDescarga = (decimal)ig.PorcentajeDescarga
                        }).ToList();
                    }
                    var escotillasData = await escotillasTask;
                    if (escotillasData != null)
                        if (escotillasData != null)
                        {
                            viewModel.EscotillasData = escotillasData.Escotillas ?? new List<EscotillaViewModel>();
                            viewModel.CapacidadTotal = escotillasData.CapacidadTotal;
                            viewModel.DescargaTotal = escotillasData.DescargaTotal;
                            viewModel.DiferenciaTotal = escotillasData.DiferenciaTotal;
                            viewModel.PorcentajeTotal = escotillasData.PorcentajeTotal;
                            viewModel.EstadoGeneral = escotillasData.EstadoGeneral;
                        }
                }
                else
                {
                    var informeGeneralTask = _movimientoService.GetInformeGeneralAsync(importacionId);
                    var escotillasTask = _movimientoService.GetEscotillasDataAsync(importacionId);
                    Task<List<Movimiento>>? calculoTask = empresaId.HasValue
                        ? _movimientoService.CalculoMovimientos(importacionId, empresaId.Value)
                        : null;

                    await Task.WhenAll(new Task[] { informeGeneralTask, escotillasTask }
                        .Concat(calculoTask != null ? new[] { calculoTask } : Enumerable.Empty<Task>()));

                    var informeGeneral = await informeGeneralTask;
                    if (informeGeneral != null)
                    {
                        viewModel.Tabla2Data = informeGeneral.Select(ig => new RegistroPesajesAgregado
                        {
                            Agroindustria = ig.Empresa ?? "Sin nombre",
                            KilosRequeridos = (decimal)ig.RequeridoKg,
                            ToneladasRequeridas = (decimal)ig.RequeridoTon,
                            DescargaKilos = (decimal)ig.DescargaKg,
                            FaltanteKilos = (decimal)ig.FaltanteKg,
                            ToneladasFaltantes = (decimal)ig.TonFaltantes,
                            CamionesFaltantes = (decimal)ig.CamionesFaltantes,
                            ConteoPlacas = ig.ConteoPlacas,
                            PorcentajeDescarga = (decimal)ig.PorcentajeDescarga
                        }).ToList();
                    }

                    var escotillasData = await escotillasTask;
                    if (escotillasData != null)
                    {
                        viewModel.EscotillasData = escotillasData.Escotillas ?? new List<EscotillaViewModel>();
                        viewModel.CapacidadTotal = escotillasData.CapacidadTotal;
                        viewModel.DescargaTotal = escotillasData.DescargaTotal;
                        viewModel.DiferenciaTotal = escotillasData.DiferenciaTotal;
                        viewModel.PorcentajeTotal = escotillasData.PorcentajeTotal;
                        viewModel.EstadoGeneral = escotillasData.EstadoGeneral;
                    }

                    if (empresaId.HasValue && calculoTask != null)
                    {
                        try
                        {
                            var calculo = await calculoTask;

                            if (calculo != null && calculo.Any())
                            {
                                viewModel.Tabla1Data = calculo
                                    .Where(c => c != null)
                                    .Select(c => new RegistroPesajesIndividual
                                    {
                                        Id = c.id,
                                        IdImportacion = c.idimportacion,
                                        IdEmpresa = c.idempresa,
                                        Guia = c.guia?.ToString() ?? string.Empty,
                                        GuiaAlterna = c.guia_alterna,
                                        Placa = c.placa,
                                        PlacaAlterna = c.placa_alterna,
                                        PesoRequerido = c.cantidadrequerida ?? 0,
                                        PesoEntregado = c.cantidadentregada,
                                        PesoFaltante = c.peso_faltante,
                                        Porcentaje = c.porcentaje,
                                        Escotilla = c.escotilla ?? 0,
                                        Bodega = c.bodega?.ToString() ?? string.Empty,
                                    }).ToList();

                                var bodegasDict = await GetBodegasDictionaryAsync();

                                viewModel.TotalesPorBodega = CalculateTotalesPorBodega(calculo, bodegasDict);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error al procesar datos de empresa {empresaId.Value}");
                            TempData["Warning"] = $"Algunos datos no pudieron cargarse: {ex.Message}";
                        }
                    }
                }

                _memoryCache.Set(cacheKey, viewModel, TimeSpan.FromMinutes(DATA_CACHE_DURATION_MINUTES));

                watch.Stop();
                _logger.LogDebug($"Tiempo de ejecución Index: {watch.ElapsedMilliseconds}ms para barco {selectedBarco}, empresa {empresaId}");

                return View(viewModel);
            }
            catch (Exception ex)
            {
                watch.Stop();
                _logger.LogError(ex, $"Error crítico al cargar RegistroPesajes. Tiempo: {watch.ElapsedMilliseconds}ms");

                try
                {
                    await PopulateDropdowns(selectedBarco, empresaId);
                }
                catch
                {
                    ViewBag.Barcos = new SelectList(Enumerable.Empty<SelectListItem>());
                    ViewBag.Empresas = new SelectList(Enumerable.Empty<SelectListItem>());
                }

                TempData["Error"] = $"Error al cargar los datos: {ex.Message}";
                return View(new RegistroPesajesViewModel());
            }
        }

        private async Task PopulateDropdowns(int? selectedBarco, int? empresaId)
        {
            try
            {
                const string barcosCacheKey = "BarcosDropdown";
                List<SelectListItem> barcos;

                if (!_memoryCache.TryGetValue(barcosCacheKey, out List<SelectListItem>? cachedBarcos))
                {
                    var barcosData = await _movimientoService.GetBarcosSelectListAsync();
                    barcos = barcosData?.ToList() ?? new List<SelectListItem>();

                    _memoryCache.Set(barcosCacheKey, barcos, TimeSpan.FromHours(BARCOS_CACHE_DURATION_HOURS));
                    _logger.LogDebug($"Actualizado caché de barcos: {barcos.Count} elementos");
                }
                else
                {
                    barcos = cachedBarcos ?? new List<SelectListItem>();
                }

                ViewBag.Barcos = new SelectList(barcos, "Value", "Text", selectedBarco);

                if (selectedBarco.HasValue)
                {
                    const string empresasCacheKey = "EmpresasDropdown";
                    List<SelectListItem> empresas;

                    if (!_memoryCache.TryGetValue(empresasCacheKey, out List<SelectListItem>? cachedEmpresas))
                    {
                        var empresasData = await _movimientoService.GetEmpresasSelectListAsync();
                        empresas = empresasData?.ToList() ?? new List<SelectListItem>();

                        _memoryCache.Set(empresasCacheKey, empresas, TimeSpan.FromHours(BARCOS_CACHE_DURATION_HOURS));
                        _logger.LogDebug($"Actualizado caché de empresas: {empresas.Count} elementos");
                    }
                    else
                    {
                        empresas = cachedEmpresas ?? new List<SelectListItem>();
                    }

                    ViewBag.Empresas = new SelectList(empresas, "Value", "Text", empresaId);
                }
                else
                {
                    ViewBag.Empresas = new SelectList(new List<SelectListItem>(), "Value", "Text");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar los dropdowns");
                throw;
            }
        }

        private async Task<Dictionary<int, string>> GetBodegasDictionaryAsync()
        {
            const string cacheKey = "BodegasDictionary";

            if (!_memoryCache.TryGetValue(cacheKey, out Dictionary<int, string>? bodegasDict) || bodegasDict == null)
            {
                var bodegas = await _bodegaService.GetAllAsync();
                bodegasDict = bodegas.ToDictionary(b => b.id, b => b.bodega ?? "Sin nombre");
                _memoryCache.Set(cacheKey, bodegasDict, TimeSpan.FromHours(BODEGAS_CACHE_DURATION_HOURS));
                _logger.LogDebug($"Actualizado caché de diccionario de bodegas: {bodegasDict.Count} elementos");
            }

            return bodegasDict ?? new Dictionary<int, string>();
        }

        private static List<TotalesPorBodegaViewModel> CalculateTotalesPorBodegaSafe(List<Movimiento> calculo, Dictionary<int, string> bodegasDict)
        {
            if (calculo == null)
            {
                return new List<TotalesPorBodegaViewModel>();
            }

            return CalculateTotalesPorBodega(calculo, bodegasDict);
        }

        private static List<TotalesPorBodegaViewModel> CalculateTotalesPorBodega(List<Movimiento> calculo, Dictionary<int, string> bodegasDict)
        {
            return calculo
                .Where(c => c.bodega.HasValue)
                .GroupBy(c => c.bodega)
                .Select(g => new TotalesPorBodegaViewModel
                {
                    Bodega = g.Key.HasValue && bodegasDict.TryGetValue(g.Key.Value, out var nombre)
                        ? nombre ?? "Sin Bodega"
                        : "Sin Bodega",
                    TotalKilos = g.Sum(m => m.cantidadentregada),
                    CantidadMovimientos = g.Count()
                })
                .ToList();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegistroPesajesIndividual viewModel)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var bodegas = await _bodegaService.GetAllAsync();
                    ViewBag.Bodegas = new SelectList(bodegas, "id", "bodega", viewModel.Bodega);
                    return View(viewModel);
                }

                var movimiento = new Movimiento
                {
                    fechahora = DateTime.Now,
                    idimportacion = viewModel.IdImportacion,
                    idempresa = viewModel.IdEmpresa,
                    tipotransaccion = 2,
                    cantidadentregada = viewModel.PesoEntregado,
                    placa = viewModel.Placa?.Trim(),
                    placa_alterna = viewModel.PlacaAlterna?.Trim(),
                    guia = !string.IsNullOrEmpty(viewModel.Guia) ? int.Parse(viewModel.Guia) : null,
                    guia_alterna = viewModel.GuiaAlterna?.Trim(),
                    escotilla = viewModel.Escotilla,
                    bodega = int.TryParse(viewModel.Bodega, out int bodegaId) ? bodegaId : null,
                    idusuario = User.GetUserId()
                };

                var result = await _movimientoService.CreateAsync(movimiento);
                TempData["Success"] = "Registro creado exitosamente.";

                InvalidateRelatedCaches(viewModel.IdImportacion, viewModel.IdEmpresa);

                return RedirectToAction(nameof(Index),
                    new { selectedBarco = viewModel.IdImportacion, empresaId = viewModel.IdEmpresa });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el registro de pesaje: {Message}", ex.Message);
                ModelState.AddModelError("", $"Error al crear el registro: {ex.Message}");

                var bodegas = await _bodegaService.GetAllAsync();
                ViewBag.Bodegas = new SelectList(bodegas, "id", "bodega", viewModel.Bodega);
                return View(viewModel);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RegistroPesajesIndividual viewModel)
        {
            try
            {
                if (id != viewModel.Id)
                {
                    return NotFound();
                }

                if (!ModelState.IsValid)
                {
                    var bodegas = await _bodegaService.GetAllAsync();
                    ViewBag.Bodegas = new SelectList(bodegas, "id", "bodega", viewModel.Bodega);
                    return View(viewModel);
                }

                var movimiento = new Movimiento
                {
                    id = viewModel.Id,
                    idimportacion = viewModel.IdImportacion,
                    idempresa = viewModel.IdEmpresa,
                    tipotransaccion = viewModel.TipoTransaccion,
                    guia = !string.IsNullOrEmpty(viewModel.Guia) ? int.Parse(viewModel.Guia) : null,
                    guia_alterna = viewModel.GuiaAlterna?.Trim(),
                    placa = viewModel.Placa?.Trim(),
                    placa_alterna = viewModel.PlacaAlterna?.Trim(),
                    cantidadentregada = viewModel.PesoEntregado,
                    escotilla = viewModel.Escotilla,
                    bodega = int.TryParse(viewModel.Bodega, out int bodegaId) ? bodegaId : null,
                    fechahora = DateTime.Now,
                    idusuario = User.GetUserId()
                };

                await _movimientoService.UpdateAsync(id, movimiento);
                TempData["Success"] = "Registro actualizado exitosamente.";

                InvalidateRelatedCaches(viewModel.IdImportacion, viewModel.IdEmpresa);

                return RedirectToAction(nameof(Index),
                    new { selectedBarco = viewModel.IdImportacion, empresaId = viewModel.IdEmpresa });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el registro de pesaje");
                ModelState.AddModelError("", $"Error al actualizar el registro: {ex.Message}");

                var bodegas = await _bodegaService.GetAllAsync();
                ViewBag.Bodegas = new SelectList(bodegas, "id", "bodega", viewModel.Bodega);
                return View(viewModel);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, int selectedBarco, int empresaId)
        {
            try
            {
                var movimiento = await _movimientoService.GetByIdAsync(id);
                if (movimiento == null)
                {
                    TempData["Error"] = "No se encontró el registro para eliminar";
                    return RedirectToAction(nameof(Index), new { selectedBarco, empresaId });
                }

                await _movimientoService.DeleteAsync(id);
                TempData["Success"] = "Registro eliminado correctamente";

                InvalidateRelatedCaches(selectedBarco, empresaId);

                return RedirectToAction(nameof(Index), new { selectedBarco, empresaId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar el registro {id}");
                TempData["Error"] = $"Error al eliminar el registro: {ex.Message}";
                return RedirectToAction(nameof(Index), new { selectedBarco, empresaId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ReporteGeneral(int? selectedBarco)
        {
            var watch = Stopwatch.StartNew();
            try
            {
                const string barcosCacheKey = "BarcosDropdown";
                List<SelectListItem> barcos;

                if (!_memoryCache.TryGetValue(barcosCacheKey, out List<SelectListItem>? cachedBarcos))
                {
                    barcos = (await _movimientoService.GetBarcosSelectListAsync())?.ToList() ?? new List<SelectListItem>();
                    _memoryCache.Set(barcosCacheKey, barcos, TimeSpan.FromHours(BARCOS_CACHE_DURATION_HOURS));
                    _logger.LogDebug($"Actualizado caché de barcos en ReporteGeneral: {barcos.Count} elementos");
                }
                else
                {
                    barcos = cachedBarcos ?? new List<SelectListItem>();
                }

                ViewBag.Barcos = new SelectList(barcos, "Value", "Text", selectedBarco);

                if (!selectedBarco.HasValue)
                {
                    return View(new ReporteEscotillasPorEmpresaViewModel());
                }

                string reporteCacheKey = $"ReporteEscotillas_{selectedBarco}";
                ReporteEscotillasPorEmpresaViewModel viewModel;

                if (!_memoryCache.TryGetValue(reporteCacheKey, out ReporteEscotillasPorEmpresaViewModel? cachedViewModel))
                {
                    viewModel = await _movimientoService.GetEscotillasPorEmpresaAsync(selectedBarco.Value);
                    _memoryCache.Set(reporteCacheKey, viewModel, TimeSpan.FromMinutes(REPORTE_CACHE_DURATION_MINUTES));
                    _logger.LogDebug($"Datos de reporte escotillas cargados de API para barco {selectedBarco}");
                }
                else
                {
                    viewModel = cachedViewModel ?? new ReporteEscotillasPorEmpresaViewModel();
                    _logger.LogDebug($"Datos de reporte escotillas recuperados de caché para barco {selectedBarco}");
                }

                var barcoSeleccionado = barcos.FirstOrDefault(b => b.Value == selectedBarco.Value.ToString());
                ViewBag.BarcoSeleccionado = barcoSeleccionado?.Text ?? "Desconocido";

                watch.Stop();
                _logger.LogDebug($"Tiempo de ejecución ReporteGeneral: {watch.ElapsedMilliseconds}ms para barco {selectedBarco}");

                return View(viewModel);
            }
            catch (Exception ex)
            {
                watch.Stop();
                _logger.LogError(ex, $"Error al cargar el reporte general. Tiempo: {watch.ElapsedMilliseconds}ms");
                TempData["Error"] = "Error al cargar los datos del reporte. Por favor, intente nuevamente.";
                return View(new ReporteEscotillasPorEmpresaViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> ReporteIndividual(string selectedBarco, int? barcoId, string returnController = "RegistroPesajes", string returnAction = "Index")
        {
            try
            {
                int? importacionId = null;

                if (!string.IsNullOrEmpty(selectedBarco) && int.TryParse(selectedBarco, out int barcoIdFromString))
                {
                    importacionId = barcoIdFromString;
                }
                else if (barcoId.HasValue)
                {
                    importacionId = barcoId.Value;
                }

                if (!importacionId.HasValue || importacionId <= 0)
                {
                    TempData["Error"] = "No se ha seleccionado una importación válida.";
                    return RedirectToAction(returnAction, returnController);
                }

                _logger.LogInformation($"Cargando ReporteIndividual para importación: {importacionId}");

                var viewModel = new RegistroPesajesViewModel();

                await PopulateDropdowns(importacionId, null);

                var movimientosTask = _movimientoService.GetAllMovimientosByImportacionAsync(importacionId.Value);
                var informeGeneralTask = _movimientoService.GetInformeGeneralAsync(importacionId.Value);
                var escotillasTask = _movimientoService.GetEscotillasDataAsync(importacionId.Value);

                await Task.WhenAll(movimientosTask, informeGeneralTask, escotillasTask);

                viewModel.Tabla1Data = await movimientosTask;

                var informeGeneral = await informeGeneralTask;
                if (informeGeneral != null)
                {
                    viewModel.Tabla2Data = informeGeneral.Select(ig => new RegistroPesajesAgregado
                    {
                        Agroindustria = ig.Empresa ?? "Sin nombre",
                        KilosRequeridos = (decimal)ig.RequeridoKg,
                        ToneladasRequeridas = (decimal)ig.RequeridoTon,
                        DescargaKilos = (decimal)ig.DescargaKg,
                        FaltanteKilos = (decimal)ig.FaltanteKg,
                        ToneladasFaltantes = (decimal)ig.TonFaltantes,
                        CamionesFaltantes = (decimal)ig.CamionesFaltantes,
                        ConteoPlacas = ig.ConteoPlacas,
                        PorcentajeDescarga = (decimal)ig.PorcentajeDescarga
                    }).ToList();
                }

                var escotillasData = await escotillasTask;
                if (escotillasData != null)
                {
                    viewModel.EscotillasData = escotillasData.Escotillas;
                    viewModel.CapacidadTotal = escotillasData.CapacidadTotal;
                    viewModel.DescargaTotal = escotillasData.DescargaTotal;
                    viewModel.DiferenciaTotal = escotillasData.DiferenciaTotal;
                    viewModel.PorcentajeTotal = escotillasData.PorcentajeTotal;
                    viewModel.EstadoGeneral = escotillasData.EstadoGeneral;
                }

                ViewBag.ReturnController = returnController;
                ViewBag.ReturnAction = returnAction;
                ViewBag.SelectedBarco = importacionId;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el reporte individual");
                TempData["Error"] = $"Error al cargar el reporte: {ex.Message}";
                return RedirectToAction(returnAction, returnController);
            }
        }

        private void InvalidateRelatedCaches(int importacionId, int empresaId)
        {
            _memoryCache.Remove($"RegistroPesajes_{importacionId}_{empresaId}");
            _memoryCache.Remove($"ReporteEscotillas_{importacionId}");
            _logger.LogDebug($"Caché invalidado para importación {importacionId}, empresa {empresaId}");
        }
    }
}