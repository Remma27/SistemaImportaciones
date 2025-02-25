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

        // GET: RegistroPesajes/Index
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

                    // Cargar datos generales que no dependen de la empresa
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

                    if (empresaId.HasValue)
                    {
                        try
                        {
                            // Obtener datos específicos de la empresa
                            var calculo = await _movimientoService.CalculoMovimientos(importacionId, empresaId.Value);

                            if (calculo != null && calculo.Any())
                            {
                                _logger.LogInformation($"Datos recibidos: {calculo.Count()} registros");

                                // Mapear nombres de bodegas
                                var bodegas = await _bodegaService.GetAllAsync();
                                var bodegasDict = bodegas.ToDictionary(b => b.id, b => b.bodega ?? "Sin nombre");

                                // Generar datos para Tabla1 (Registros Individuales)
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

                                // Generar datos para TotalesPorBodega sin filtrar por tipo de transacción
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

                    // Mantener el código existente para la información del barco y escotillas
                    var importacion = await _importacionService.GetByIdAsync(selectedBarco.Value);
                    if (importacion?.Barco != null)
                    {
                        var barco = importacion.Barco;
                        viewModel.Barco = barco;

                        // Obtener totales (descargado se suma de las empresas)
                        decimal totalDescargado = viewModel.Tabla2Data.Sum(x => x.DescargaKilos);
                        decimal totalRequerido = viewModel.Tabla2Data.Sum(x => x.KilosRequeridos);

                        // Calcular capacidad total del barco (es decir, la suma de las escotillas)
                        decimal capacidadTotal = 0;
                        var escotillasActivas = new Dictionary<int, decimal>();

                        // Iterar hasta escotilla7 (según se tenga configurado)
                        for (int i = 1; i <= 7; i++)
                        {
                            var capacidad = (decimal?)barco.GetType().GetProperty($"escotilla{i}")?.GetValue(barco) ?? 0;
                            if (capacidad > 0)
                            {
                                escotillasActivas.Add(i, capacidad);
                                capacidadTotal += capacidad;
                            }
                        }

                        // Distribuir la descarga total de forma proporcional al valor de cada escotilla
                        viewModel.EscotillasData = new List<EscotillaViewModel>();
                        foreach (var escotilla in escotillasActivas)
                        {
                            // El valor "requerido" para la escotilla es su capacidad
                            decimal requeridaEscotilla = escotilla.Value;

                            // Proporción que representa la escotilla en el total
                            decimal proporcion = capacidadTotal > 0 ? escotilla.Value / capacidadTotal : 0;

                            // Se distribuye la descarga total según la misma proporción
                            decimal descargaReal = Math.Round(totalDescargado * proporcion, 0);

                            // Diferencia: faltante (positivo) o excedente (negativo)
                            decimal diferencia = requeridaEscotilla - descargaReal;

                            // Porcentaje de descarga para la escotilla (evitar división por cero)
                            decimal porcentaje = requeridaEscotilla > 0
                                ? Math.Round((descargaReal * 100.0M / requeridaEscotilla), 2)
                                : 0;

                            viewModel.EscotillasData.Add(new EscotillaViewModel
                            {
                                Numero = escotilla.Key,
                                Capacidad = escotilla.Value,
                                DescargaEsperada = requeridaEscotilla,
                                DescargaReal = descargaReal,
                                Diferencia = diferencia,
                                Porcentaje = porcentaje
                            });
                        }

                        // Actualizar totales del barco
                        viewModel.TotalKilosRequeridos = capacidadTotal;
                        viewModel.TotalDescargaKilos = totalDescargado;
                        viewModel.TotalKilosFaltantes = capacidadTotal - totalDescargado;
                        viewModel.PorcentajeTotal = capacidadTotal > 0
                            ? Math.Round((totalDescargado * 100.0M / capacidadTotal), 2)
                            : 0;
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

        // GET: RegistroPesajes/Create
        public async Task<IActionResult> Create(int? selectedBarco, int? empresaId)
        {
            if (!selectedBarco.HasValue || !empresaId.HasValue)
            {
                return RedirectToAction(nameof(Index));
            }

            // Cargar información del barco seleccionado
            var barcos = await _movimientoService.GetBarcosSelectListAsync();
            var selectedBarcoItem = barcos.FirstOrDefault(b => b.Value == selectedBarco.Value.ToString());
            ViewBag.NombreBarco = selectedBarcoItem?.Text ?? "Desconocido";

            // Cargar lista de bodegas
            var bodegas = await _bodegaService.GetAllAsync();
            ViewBag.Bodegas = new SelectList(bodegas, "id", "bodega");

            // Preparar el viewModel con los datos necesarios
            var viewModel = new RegistroPesajesIndividual
            {
                FechaHora = DateTime.Now,
                IdImportacion = selectedBarco.Value,
                IdEmpresa = empresaId.Value,
                TipoTransaccion = 2
            };

            return View(viewModel);
        }

        // POST: RegistroPesajes/Create
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

        // GET: RegistroPesajes/Edit/5
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
                    Guia = movimiento.guia?.ToString(),
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

        // POST: RegistroPesajes/Edit/5
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

        // GET: RegistroPesajes/Delete/5
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
                    Guia = movimiento.guia?.ToString(),
                    GuiaAlterna = movimiento.guia_alterna,
                    Placa = movimiento.placa,
                    PlacaAlterna = movimiento.placa_alterna,
                    PesoEntregado = movimiento.cantidadentregada,
                    Escotilla = movimiento.escotilla ?? 0
                };

                // Get bodega name for display
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

        // POST: RegistroPesajes/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, int selectedBarco, int empresaId)
        {
            try
            {
                _logger.LogInformation($"Iniciando proceso de eliminación para registro {id}");

                // Verificar que el registro existe antes de intentar eliminarlo
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
    }
}