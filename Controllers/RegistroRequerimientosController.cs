using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sistema_de_Gestion_de_Importaciones.ViewModels;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using Sistema_de_Gestion_de_Importaciones.Helpers;
using API.Models;
using Sistema_de_Gestion_de_Importaciones.Extensions;
using System.Text.Json;

namespace Sistema_de_Gestion_de_Importaciones.Controllers
{
    [Authorize]
    public class RegistroRequerimientosController : Controller
    {
        private readonly IMovimientoService _movimientoService;
        private readonly ILogger<RegistroRequerimientosController> _logger;

        public RegistroRequerimientosController(IMovimientoService registroService,
                                                ILogger<RegistroRequerimientosController> logger)
        {
            _movimientoService = registroService;
            _logger = logger;
        }

        [Authorize(Roles = "Administrador,Operador")]
        public async Task<IActionResult> Index(int? selectedBarco)
        {
            try
            {
                var barcos = await _movimientoService.GetBarcosSelectListAsync();
                ViewBag.Barcos = new SelectList(barcos, "Value", "Text", selectedBarco);

                var registros = await _movimientoService.GetRegistroRequerimientosAsync(selectedBarco ?? 0);
                return View(registros);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading registro requerimientos: {Message}", ex.Message);
                this.Error("Error al cargar los registros: " + ex.Message);
                return View(new List<RegistroRequerimientosViewModel>());
            }
        }

        [Authorize(Roles = "Administrador,Operador")]
        public async Task<IActionResult> Create(int? selectedBarco)
        {
            if (!selectedBarco.HasValue)
            {
                return RedirectToAction(nameof(Index));
            }

            var barcos = await _movimientoService.GetBarcosSelectListAsync();
            var selectedBarcoItem = barcos.FirstOrDefault(b => b.Value == selectedBarco.Value.ToString());
            ViewBag.NombreBarco = selectedBarcoItem != null ? selectedBarcoItem.Text : "Desconocido";

            ViewBag.IdImportacion = selectedBarco.Value;

            ViewBag.Empresas = new SelectList(await _movimientoService.GetEmpresasSelectListAsync(), "Value", "Text");

            var viewModel = new RegistroRequerimientosViewModel
            {
                FechaHora = DateTime.Now,
                IdImportacion = selectedBarco.Value,
                TipoTransaccion = 1
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Operador")]
        public async Task<IActionResult> Create(RegistroRequerimientosViewModel viewModel, int? selectedBarco)
        {
            if (viewModel.IdImportacion == null && selectedBarco.HasValue)
            {
                viewModel.IdImportacion = selectedBarco.Value;
            }

            foreach (var key in ModelState.Keys.ToList())
            {
                var state = ModelState[key];
                if (state != null && state.Errors.Count > 0)
                {
                    foreach (var error in state.Errors.ToList())
                    {
                        _logger.LogError($"Validation error for '{key}': {error.ErrorMessage}");
                    }
                }
            }

            _logger.LogInformation($"Received viewModel: FechaHora={viewModel.FechaHora}, " +
                $"IdImportacion={viewModel.IdImportacion}, IdEmpresa={viewModel.IdEmpresa}, " +
                $"CantidadRequerida={viewModel.CantidadRequerida}, CantidadCamiones={viewModel.CantidadCamiones}");

            if (!ModelState.IsValid)
            {
                this.Error("Error al crear el requerimiento. Revise los campos obligatorios.");

                ViewBag.Empresas = new SelectList(await _movimientoService.GetEmpresasSelectListAsync(), "Value", "Text", viewModel.IdEmpresa);
                ViewBag.IdImportacion = viewModel.IdImportacion ?? selectedBarco;
                ViewBag.NombreBarco = (await _movimientoService.GetBarcosSelectListAsync())
                    .FirstOrDefault(b => b.Value == (viewModel.IdImportacion ?? selectedBarco).ToString())?.Text ?? "Desconocido";

                return View(viewModel);
            }

            var movimiento = new Movimiento
            {
                id = viewModel.IdMovimiento ?? 0,
                idimportacion = viewModel.IdImportacion ?? 0,
                idempresa = viewModel.IdEmpresa,
                tipotransaccion = viewModel.TipoTransaccion ?? 1,
                cantidadrequerida = viewModel.CantidadRequerida,
                cantidadcamiones = viewModel.CantidadCamiones,
                fechahora = viewModel.FechaHora ?? DateTime.Now,
                idusuario = HttpContext.User.GetUserId()
            };

            try
            {
                await _movimientoService.CreateAsync(movimiento);
                this.Success("Requerimiento registrado exitosamente");
                return RedirectToAction(nameof(Index), new { selectedBarco = viewModel.IdImportacion });
            }
            catch (Exception ex)
            {
                this.Error("Error al crear el movimiento: " + ex.Message);
                _logger.LogError(ex, "Error al crear el movimiento");
                ModelState.AddModelError("", "Ocurrió un error al crear el movimiento.");
                ViewBag.Empresas = new SelectList(await _movimientoService.GetEmpresasSelectListAsync(), "Value", "Text");
                ViewBag.IdImportacion = viewModel.IdImportacion;
                ViewBag.NombreBarco = (await _movimientoService.GetBarcosSelectListAsync())
                    .FirstOrDefault(b => b.Value == viewModel.IdImportacion.ToString())?.Text ?? "Desconocido";
                return View(viewModel);
            }
        }

        [HttpGet]
        [Authorize(Roles = "Administrador,Operador")]
        public async Task<IActionResult> Edit(int? id, int? selectedBarco)
        {
            if (id == null)
                return NotFound();

            var viewModel = await _movimientoService.GetRegistroRequerimientoByIdAsync(id.Value);
            if (viewModel == null)
                return NotFound();

            ViewBag.Empresas = new SelectList(await _movimientoService.GetEmpresasSelectListAsync(), "Value", "Text", viewModel.IdEmpresa);
            ViewBag.SelectedBarco = selectedBarco;

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Operador")]
        public async Task<IActionResult> Edit(int id, RegistroRequerimientosViewModel viewModel, int? selectedBarco)
        {
            try
            {
                if (id != viewModel.IdMovimiento)
                {
                    TempData["Error"] = "ID mismatch error.";
                    return RedirectToAction(nameof(Index), new { selectedBarco });
                }

                if (ModelState.IsValid)
                {
                    var userId = HttpContext.User.GetUserId();
                    await _movimientoService.UpdateAsync(id, viewModel, userId.ToString());

                    this.Success("Registro actualizado correctamente");
                    return RedirectToAction(nameof(Index), new { selectedBarco });
                }
                else
                {
                    foreach (var modelState in ModelState.Values)
                    {
                        foreach (var error in modelState.Errors)
                        {
                            _logger.LogError($"Validation error: {error.ErrorMessage}");
                        }
                    }

                    ViewBag.Empresas = new SelectList(await _movimientoService.GetEmpresasSelectListAsync(), "Value", "Text", viewModel.IdEmpresa);
                    ViewBag.SelectedBarco = selectedBarco;
                    return View(viewModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating movimiento ID {id}: {ex.Message}");
                ModelState.AddModelError("", $"Ha ocurrido un error al guardar los cambios: {ex.Message}");

                ViewBag.Empresas = new SelectList(await _movimientoService.GetEmpresasSelectListAsync(), "Value", "Text", viewModel.IdEmpresa);
                ViewBag.SelectedBarco = selectedBarco;
                return View(viewModel);
            }
        }

        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int? id, int? selectedBarco)
        {
            if (id == null)
                return NotFound();

            var viewModel = await _movimientoService.GetRegistroRequerimientoByIdAsync(id.Value);
            if (viewModel == null)
                return NotFound();

            _logger.LogInformation($"Delete action - Initial viewModel values: " +
                                  $"IdEmpresa={viewModel.IdEmpresa}, Empresa={viewModel.Empresa}, " +
                                  $"IdImportacion={viewModel.IdImportacion}, Importacion={viewModel.Importacion}");

            ViewBag.SelectedBarco = selectedBarco;
            return View(viewModel);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id, int? selectedBarco)
        {
            try
            {
                await _movimientoService.DeleteAsync(id);
                this.Success("Requerimiento eliminado exitosamente");
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("400") && 
                (ex.Message.Contains("relacionado") || ex.Message.Contains("asociado") || 
                ex.Message.Contains("depende") || ex.Message.Contains("utilizado")))
            {
                _logger.LogWarning(ex, $"Intento de eliminar movimiento con ID: {id} que tiene dependencias");
                this.Warning("No se puede eliminar este requerimiento porque tiene dependencias.");
                return RedirectToAction(nameof(Index), new { selectedBarco });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("relacionado") || 
                ex.Message.Contains("asociado") || ex.Message.Contains("depende") || 
                ex.Message.Contains("utilizado"))
            {
                _logger.LogWarning(ex, $"Intento de eliminar movimiento con ID: {id} con dependencias");
                this.Warning(ex.Message);
                return RedirectToAction(nameof(Index), new { selectedBarco });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting movimiento ID {Id}: {Message}", id, ex.Message);
                this.Error("Error al eliminar el requerimiento: " + ex.Message);
            }
            return RedirectToAction(nameof(Index), new { selectedBarco });
        }
    }
}


