using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sistema_de_Gestion_de_Importaciones.ViewModels;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using API.Models;
using Sistema_de_Gestion_de_Importaciones.Helpers;
using System.Text.Json;

namespace Sistema_de_Gestion_de_Importaciones.Controllers
{
    public class RegistroPesajesController : Controller
    {
        private readonly IMovimientoService _movimientoService;
        private readonly IBodegaService _bodegaService;
        private readonly ILogger<RegistroPesajesController> _logger;
        private readonly IImportacionService _importacionService;

        public RegistroPesajesController(
            IMovimientoService movimientoService,
            IBodegaService bodegaService,
            ILogger<RegistroPesajesController> logger,
            IImportacionService importacionService)
        {
            _movimientoService = movimientoService;
            _bodegaService = bodegaService;
            _logger = logger;
            _importacionService = importacionService;
        }

        public async Task<IActionResult> Index(int? selectedBarco, int? empresaId)
        {
            try
            {
                // Cargar dropdowns
                var barcosList = await _movimientoService.GetBarcosSelectListAsync();
                ViewBag.Barcos = new SelectList(barcosList, "Value", "Text", selectedBarco);
                ViewBag.Empresas = new SelectList(await _movimientoService.GetEmpresasSelectListAsync(), "Value", "Text", empresaId);

                var viewModel = new RegistroPesajesViewModel
                {
                    Tabla1Data = new List<RegistroPesajesIndividual>(),
                    Tabla2Data = new List<RegistroPesajesAgregado>(),
                    TotalesPorBodega = new List<TotalesPorBodegaViewModel>(),
                    EscotillasData = new List<EscotillaViewModel>()
                };

                if (selectedBarco.HasValue)
                {
                    int importacionId = selectedBarco.Value;

                    var informeGeneral = await _movimientoService.GetInformeGeneralAsync(importacionId);
                    viewModel.Tabla2Data = informeGeneral.Select(ig => new RegistroPesajesAgregado
                    {
                        Agroindustria = ig.Empresa,
                        KilosRequeridos = (decimal)ig.RequeridoKg,
                        ToneladasRequeridas = (decimal)ig.RequeridoTon,
                        DescargaKilos = (decimal)ig.DescargaKg,
                        FaltanteKilos = (decimal)ig.FaltanteKg,
                        ToneladasFaltantes = (decimal)ig.TonFaltantes,
                        CamionesFaltantes = (decimal)ig.CamionesFaltantes,
                        ConteoPlacas = ig.ConteoPlacas,
                        PorcentajeDescarga = (decimal)ig.PorcentajeDescarga
                    }).ToList();

                    using (var client = new HttpClient())
                    {
                        try
                        {
                            var response = await client.GetAsync($"http://localhost:5079/api/Movimientos/CalculoEscotillas?importacionId={importacionId}");
                            if (response.IsSuccessStatusCode)
                            {
                                var result = await response.Content.ReadFromJsonAsync<EscotillasResponse>();
                                if (result != null)
                                {
                                    viewModel.EscotillasData = result.escotillas;
                                    viewModel.CapacidadTotal = result.totales.capacidadTotal;
                                    viewModel.DescargaTotal = result.totales.descargaTotal;
                                    viewModel.DiferenciaTotal = result.totales.diferenciaTotal;
                                    viewModel.PorcentajeTotal = result.totales.porcentajeTotal;
                                    viewModel.EstadoGeneral = result.totales.estadoGeneral;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error al obtener datos de escotillas");
                        }
                    }

                    if (empresaId.HasValue)
                    {
                        try
                        {
                            var calculo = await _movimientoService.CalculoMovimientos(importacionId, empresaId.Value);

                            if (calculo != null && calculo.Any())
                            {
                                _logger.LogInformation($"Datos recibidos: {calculo.Count()} registros");

                                var bodegas = await _bodegaService.GetAllAsync();
                                var bodegasDict = bodegas.ToDictionary(b => b.id, b => b.bodega ?? "Sin nombre");

                                viewModel.Tabla1Data = calculo
                                    .Select(c => new RegistroPesajesIndividual
                                    {
                                        Id = c.id,
                                        IdImportacion = importacionId,
                                        IdEmpresa = empresaId.Value,
                                        Bodega = c.bodega.HasValue && bodegasDict.ContainsKey(c.bodega.Value)
                                            ? bodegasDict[c.bodega.Value]
                                            : "Sin Bodega",
                                        Guia = c.guia?.ToString() ?? string.Empty,
                                        GuiaAlterna = c.guia_alterna ?? string.Empty,
                                        Placa = c.placa ?? string.Empty,
                                        PlacaAlterna = c.placa_alterna ?? string.Empty,
                                        PesoEntregado = c.cantidadentregada,
                                        PesoRequerido = c.cantidadrequerida.GetValueOrDefault(),
                                        PesoFaltante = c.peso_faltante,
                                        Porcentaje = c.porcentaje,
                                        TipoTransaccion = c.tipotransaccion,
                                        Escotilla = c.escotilla ?? 0
                                    })
                                    .ToList();

                                viewModel.TotalesPorBodega = calculo
                                    .Where(c => c.bodega.HasValue)
                                    .GroupBy(c => c.bodega)
                                    .Select(g => new TotalesPorBodegaViewModel
                                    {
                                        Bodega = g.Key.HasValue && bodegasDict.ContainsKey(g.Key.Value)
                                            ? bodegasDict[g.Key.Value]
                                            : "Sin Bodega",
                                        TotalKilos = g.Sum(m => m.cantidadentregada),
                                        CantidadMovimientos = g.Count()
                                    })
                                    .ToList();

                                _logger.LogInformation($"Registros cargados - Individuales: {viewModel.Tabla1Data.Count}, Bodegas: {viewModel.TotalesPorBodega.Count}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error al cargar datos específicos de la empresa");
                        }
                    }
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar la página de registro de pesajes");
                return View(new RegistroPesajesViewModel
                {
                    EscotillasData = new List<EscotillaViewModel>()
                });
            }
        }

        public async Task<IActionResult> Create(int? selectedBarco, int? empresaId)
        {
            if (!selectedBarco.HasValue || !empresaId.HasValue)
            {
                return RedirectToAction(nameof(Index));
            }

            var barcos = await _movimientoService.GetBarcosSelectListAsync();
            var selectedBarcoItem = barcos.FirstOrDefault(b => b.Value == selectedBarco.Value.ToString());
            ViewBag.NombreBarco = selectedBarcoItem?.Text ?? "Desconocido";

            var bodegas = await _bodegaService.GetAllAsync();
            ViewBag.Bodegas = new SelectList(bodegas, "id", "bodega");

            var viewModel = new RegistroPesajesIndividual
            {
                FechaHora = DateTime.Now,
                IdImportacion = selectedBarco.Value,
                IdEmpresa = empresaId.Value,
                TipoTransaccion = 2
            };

            return View(viewModel);
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

                _logger.LogInformation($"Creando nuevo registro: {JsonSerializer.Serialize(viewModel)}");

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

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var movimiento = await _movimientoService.GetByIdAsync(id.Value);

                if (movimiento == null)
                {
                    TempData["Error"] = $"No se encontró el registro con ID {id}";
                    return RedirectToAction(nameof(Index));
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
                    Bodega = movimiento.bodega?.ToString() ?? string.Empty
                };

                var bodegas = await _bodegaService.GetAllAsync();
                ViewBag.Bodegas = new SelectList(bodegas, "id", "bodega", viewModel.Bodega);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener el registro {id} para editar");
                TempData["Error"] = "Ocurrió un error al cargar el registro para editar";
                return RedirectToAction(nameof(Index));
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

        public async Task<IActionResult> Delete(int? id, int? selectedBarco, int? empresaId)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var movimiento = await _movimientoService.GetByIdAsync(id.Value);
                if (movimiento == null)
                {
                    TempData["Error"] = $"No se encontró el registro con ID {id}";
                    return RedirectToAction(nameof(Index), new { selectedBarco, empresaId });
                }

                var viewModel = new RegistroPesajesIndividual
                {
                    Id = movimiento.id,
                    IdImportacion = selectedBarco ?? movimiento.idimportacion,
                    IdEmpresa = empresaId ?? movimiento.idempresa,
                    Guia = movimiento.guia?.ToString() ?? string.Empty,
                    GuiaAlterna = movimiento.guia_alterna,
                    Placa = movimiento.placa,
                    PlacaAlterna = movimiento.placa_alterna,
                    PesoEntregado = movimiento.cantidadentregada,
                    Escotilla = movimiento.escotilla ?? 0
                };

                if (movimiento.bodega.HasValue)
                {
                    var bodegas = await _bodegaService.GetAllAsync();
                    var bodega = bodegas.FirstOrDefault(b => b.id == movimiento.bodega.Value);
                    viewModel.Bodega = bodega?.bodega ?? "N/A";
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al cargar el registro {id} para eliminar");
                TempData["Error"] = "Error al cargar el registro para eliminar";
                return RedirectToAction(nameof(Index), new { selectedBarco, empresaId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, int selectedBarco, int empresaId)
        {
            try
            {
                _logger.LogInformation($"Iniciando proceso de eliminación para registro {id}");

                var movimiento = await _movimientoService.GetByIdAsync(id);
                if (movimiento == null)
                {
                    _logger.LogWarning($"No se encontró el registro {id} para eliminar");
                    TempData["Error"] = "No se encontró el registro para eliminar";
                    return RedirectToAction(nameof(Index), new { selectedBarco, empresaId });
                }

                await _movimientoService.DeleteAsync(id);

                _logger.LogInformation($"Registro {id} eliminado exitosamente");
                TempData["Success"] = "Registro eliminado correctamente";

                return RedirectToAction(nameof(Index), new { selectedBarco, empresaId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar el registro {id}");
                TempData["Error"] = $"Error al eliminar el registro: {ex.Message}";
                return RedirectToAction(nameof(Index), new { selectedBarco, empresaId });
            }
        }

        public async Task<IActionResult> ReporteIndividual(int? selectedBarco)
        {
            try
            {
                var viewModel = new RegistroPesajesViewModel();

                if (selectedBarco.HasValue)
                {
                    int importacionId = selectedBarco.Value;
                    var movimientos = await _movimientoService.GetAllMovimientosByImportacionAsync(importacionId);
                    viewModel.Tabla1Data = movimientos;
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el reporte individual");
                return View(new RegistroPesajesViewModel());
            }
        }
    }
}