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

        public async Task<IActionResult> Index(int? selectedBarco, int? empresaId, bool refreshData = false)
        {
            var watch = Stopwatch.StartNew();
            try
            {
                // Disable caching for this response
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";

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

                int importacionId = selectedBarco.Value;

                try
                {
                    await LoadEscotillasData(viewModel, importacionId, true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error al cargar datos de escotillas, continuando con el resto de datos");
                    SetDefaultEscotillasValues(viewModel, "Error al cargar datos");
                }

                try
                {
                    await LoadTabla2Data(viewModel, importacionId, true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error al cargar datos de tabla 2, continuando con el resto de datos");
                    viewModel.Tabla2Data = new List<RegistroPesajesAgregado>();
                }

                if (empresaId.HasValue)
                {
                    try
                    {
                        await LoadTabla1Data(viewModel, importacionId, empresaId.Value, true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error al cargar datos de tabla 1, continuando sin estos datos");
                        viewModel.Tabla1Data = new List<RegistroPesajesIndividual>();
                    }
                }

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

        private async Task LoadTabla1Data(RegistroPesajesViewModel viewModel, int importacionId, int empresaId, bool refreshData = false)
        {
            try
            {
                // Always fetch fresh data from the database
                _logger.LogInformation($"Cargando datos frescos para tabla 1, importación: {importacionId}, empresa: {empresaId}");
                var calculo = await _movimientoService.CalculoMovimientos(importacionId, empresaId);

                if (calculo != null && calculo.Any())
                {
                    var bodegasDict = await GetBodegasDictionaryAsync();

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
                            Bodega = c.bodega.HasValue && bodegasDict.TryGetValue(c.bodega.Value, out var nombreBodega)
                                ? nombreBodega
                                : c.bodega?.ToString() ?? string.Empty,
                        }).ToList();

                    viewModel.TotalesPorBodega = CalculateTotalesPorBodega(calculo, bodegasDict);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al cargar datos de tabla 1 para empresa {empresaId}");
                throw;
            }
        }

        private async Task LoadTabla2Data(RegistroPesajesViewModel viewModel, int importacionId, bool refreshData)
        {
            try
            {
                // Always fetch fresh data from the database
                _logger.LogInformation($"Cargando datos frescos para tabla 2, importación: {importacionId}");
                var informeGeneral = await _movimientoService.GetInformeGeneralAsync(importacionId);

                // Log the TotalMovimientos value after API call
                _logger.LogInformation($"TotalMovimientos retrieved from API: {_movimientoService.TotalMovimientos}");

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

                    // Also store the value in ViewBag for debugging purposes
                    ViewBag.TotalMovimientos = _movimientoService.TotalMovimientos;
                    _logger.LogInformation($"ViewBag.TotalMovimientos set to: {_movimientoService.TotalMovimientos}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al cargar datos de informe general para importación {importacionId}");
                throw;
            }
        }

        private async Task LoadEscotillasData(RegistroPesajesViewModel viewModel, int importacionId, bool refreshData)
        {
            try
            {
                // Always fetch fresh data from the database
                _logger.LogInformation($"Cargando datos frescos de escotillas para importación {importacionId}");
                var escotillasData = await _movimientoService.GetEscotillasDataAsync(importacionId);

                if (escotillasData == null)
                {
                    _logger.LogWarning($"API devolvió NULL para datos de escotillas de importación {importacionId}");
                    SetDefaultEscotillasValues(viewModel, $"No hay datos disponibles para la importación {importacionId}");
                    return;
                }

                _logger.LogInformation($"TotalKilosRequeridos recibidos: {escotillasData.TotalKilosRequeridos}");

                viewModel.EscotillasData = escotillasData.Escotillas;
                viewModel.CapacidadTotal = escotillasData.CapacidadTotal;
                viewModel.DescargaTotal = escotillasData.DescargaTotal;
                viewModel.DiferenciaTotal = escotillasData.DiferenciaTotal;
                viewModel.PorcentajeTotal = escotillasData.PorcentajeTotal;
                viewModel.EstadoGeneral = escotillasData.EstadoGeneral;
                viewModel.TotalKilosRequeridos = escotillasData.TotalKilosRequeridos;

                // Establecer ViewData que será utilizado por la vista parcial
                ViewData["KilosRequeridos"] = escotillasData.TotalKilosRequeridos;
                ViewData["EstadoGeneral"] = escotillasData.EstadoGeneral;

                _logger.LogInformation($"ViewData[KilosRequeridos] establecido a: {ViewData["KilosRequeridos"]}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al cargar datos de escotillas para importación {importacionId}: {ex.Message}");
                SetDefaultEscotillasValues(viewModel, $"Error al cargar datos de escotillas: {ex.Message}");
            }
        }

        private void SetDefaultEscotillasValues(RegistroPesajesViewModel viewModel, string estado)
        {
            viewModel.EscotillasData = new List<EscotillaViewModel>();
            viewModel.CapacidadTotal = 0;
            viewModel.DescargaTotal = 0;
            viewModel.DiferenciaTotal = 0;
            viewModel.PorcentajeTotal = 0;
            viewModel.EstadoGeneral = estado;
            viewModel.TotalKilosRequeridos = 0;
        }

        private async Task PopulateDropdowns(int? selectedBarco, int? empresaId)
        {
            try
            {
                var barcosData = await _movimientoService.GetBarcosSelectListAsync();
                var barcos = barcosData?.ToList() ?? new List<SelectListItem>();
                ViewBag.Barcos = new SelectList(barcos, "Value", "Text", selectedBarco);

                if (selectedBarco.HasValue)
                {
                    var empresasData = await _movimientoService.GetEmpresasSelectListAsync();
                    var empresas = empresasData?.ToList() ?? new List<SelectListItem>();
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
            var bodegas = await _bodegaService.GetAllAsync();
            return bodegas.ToDictionary(b => b.id, b => b.bodega ?? "Sin nombre");
        }

        private static List<TotalesPorBodegaViewModel> CalculateTotalesPorBodega(List<Movimiento> calculo, Dictionary<int, string> bodegasDict)
        {
            var bodegasTotales = bodegasDict.ToDictionary(
                kvp => kvp.Key,
                kvp => new TotalesPorBodegaViewModel
                {
                    Bodega = kvp.Value ?? "Sin Bodega",
                    TotalKilos = 0,
                    CantidadMovimientos = 0
                }
            );

            foreach (var movimiento in calculo.Where(c => c.bodega.HasValue))
            {
                if (movimiento.bodega.HasValue && bodegasTotales.TryGetValue(movimiento.bodega.Value, out var bodegaViewModel))
                {
                    bodegaViewModel.TotalKilos += movimiento.cantidadentregada;
                    bodegaViewModel.CantidadMovimientos += 1;
                }
            }

            return bodegasTotales.Values.ToList();
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

                // Always force data reload after create
                return RedirectToAction(nameof(Index),
                    new { selectedBarco = viewModel.IdImportacion, empresaId = viewModel.IdEmpresa, refreshData = true });
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

                // Always force data reload after edit
                return RedirectToAction(nameof(Index),
                    new { selectedBarco = viewModel.IdImportacion, empresaId = viewModel.IdEmpresa, refreshData = true });
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

        [HttpGet]
        public async Task<IActionResult> Edit(int id, int selectedBarco, int empresaId)
        {
            try
            {
                _logger.LogInformation($"Cargando formulario de edición para movimiento ID: {id}");

                var movimiento = await _movimientoService.GetByIdAsync(id);
                if (movimiento == null)
                {
                    _logger.LogWarning($"No se encontró el movimiento con ID: {id}");
                    TempData["Error"] = "No se encontró el registro para editar";
                    return RedirectToAction(nameof(Index), new { selectedBarco, empresaId });
                }

                var viewModel = new RegistroPesajesIndividual
                {
                    Id = movimiento.id,
                    IdImportacion = movimiento.idimportacion,
                    IdEmpresa = movimiento.idempresa,
                    TipoTransaccion = movimiento.tipotransaccion,
                    Guia = movimiento.guia?.ToString() ?? string.Empty,
                    GuiaAlterna = movimiento.guia_alterna,
                    Placa = movimiento.placa,
                    PlacaAlterna = movimiento.placa_alterna,
                    PesoEntregado = movimiento.cantidadentregada,
                    Escotilla = movimiento.escotilla ?? 0,
                    Bodega = movimiento.bodega?.ToString() ?? string.Empty,
                };

                var bodegas = await _bodegaService.GetAllAsync();
                ViewBag.Bodegas = new SelectList(bodegas, "id", "bodega", viewModel.Bodega);

                ViewBag.SelectedBarco = selectedBarco;
                ViewBag.EmpresaId = empresaId;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al cargar el formulario de edición para movimiento ID: {id}");
                TempData["Error"] = $"Error al cargar el formulario: {ex.Message}";
                return RedirectToAction(nameof(Index), new { selectedBarco, empresaId });
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

                // Always force data reload after delete
                return RedirectToAction(nameof(Index), new { selectedBarco, empresaId, refreshData = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar el registro {id}");
                TempData["Error"] = $"Error al eliminar el registro: {ex.Message}";
                return RedirectToAction(nameof(Index), new { selectedBarco, empresaId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id, int selectedBarco, int empresaId)
        {
            try
            {
                _logger.LogInformation($"Cargando formulario de eliminación para movimiento ID: {id}");

                var movimiento = await _movimientoService.GetByIdAsync(id);
                if (movimiento == null)
                {
                    _logger.LogWarning($"No se encontró el movimiento con ID: {id}");
                    TempData["Error"] = "No se encontró el registro para eliminar";
                    return RedirectToAction(nameof(Index), new { selectedBarco, empresaId });
                }

                var viewModel = new RegistroPesajesIndividual
                {
                    Id = movimiento.id,
                    IdImportacion = movimiento.idimportacion,
                    IdEmpresa = movimiento.idempresa,
                    Guia = movimiento.guia?.ToString() ?? string.Empty,
                    GuiaAlterna = movimiento.guia_alterna,
                    Placa = movimiento.placa,
                    PlacaAlterna = movimiento.placa_alterna,
                    PesoEntregado = movimiento.cantidadentregada,
                    Escotilla = movimiento.escotilla ?? 0,
                    Bodega = movimiento.bodega?.ToString() ?? string.Empty,
                };

                ViewBag.SelectedBarco = selectedBarco;
                ViewBag.EmpresaId = empresaId;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al cargar el formulario de eliminación para movimiento ID: {id}");
                TempData["Error"] = $"Error al cargar el formulario: {ex.Message}";
                return RedirectToAction(nameof(Index), new { selectedBarco, empresaId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ReporteGeneral(int? selectedBarco)
        {
            var watch = Stopwatch.StartNew();
            try
            {
                // Disable caching for this response
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";

                var barcosData = await _movimientoService.GetBarcosSelectListAsync();
                var barcos = barcosData?.ToList() ?? new List<SelectListItem>();
                ViewBag.Barcos = new SelectList(barcos, "Value", "Text", selectedBarco);

                if (!selectedBarco.HasValue)
                {
                    return View(new ReporteEscotillasPorEmpresaViewModel());
                }

                var viewModel = await _movimientoService.GetEscotillasPorEmpresaAsync(selectedBarco.Value);

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
                // Disable caching for this response
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";

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

        [HttpGet]
        public async Task<IActionResult> Create(int selectedBarco, int empresaId)
        {
            try
            {
                _logger.LogInformation($"Cargando formulario de creación para barco: {selectedBarco}, empresa: {empresaId}");

                // Crear un nuevo viewModel inicializado con los IDs necesarios
                var viewModel = new RegistroPesajesIndividual
                {
                    IdImportacion = selectedBarco,
                    IdEmpresa = empresaId,
                    TipoTransaccion = 2, // Valor predeterminado para tipo de transacción
                };

                // Cargar las bodegas para el dropdown
                var bodegas = await _bodegaService.GetAllAsync();
                ViewBag.Bodegas = new SelectList(bodegas, "id", "bodega");

                // Pasar los parámetros de ruta al ViewBag para mantener el contexto
                ViewBag.SelectedBarco = selectedBarco;
                ViewBag.EmpresaId = empresaId;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al cargar el formulario de creación para barco: {selectedBarco}, empresa: {empresaId}");
                TempData["Error"] = $"Error al cargar el formulario: {ex.Message}";
                return RedirectToAction(nameof(Index), new { selectedBarco, empresaId });
            }
        }
    }
}