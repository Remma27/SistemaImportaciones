using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sistema_de_Gestion_de_Importaciones.ViewModels;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using Sistema_de_Gestion_de_Importaciones.Helpers;
using API.Models;
using Sistema_de_Gestion_de_Importaciones.Extensions;

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
        public async Task<IActionResult> Create(RegistroRequerimientosViewModel viewModel, int? selectedBarco)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Empresas = new SelectList(await _movimientoService.GetEmpresasSelectListAsync(), "Value", "Text");
                ViewBag.IdImportacion = viewModel.IdImportacion;
                ViewBag.NombreBarco = (await _movimientoService.GetBarcosSelectListAsync())
                    .FirstOrDefault(b => b.Value == viewModel.IdImportacion.ToString())?.Text ?? "Desconocido";
                return View(viewModel);
            }

            var movimiento = new Movimiento
            {
                fechahora = viewModel.FechaHora ?? DateTime.Now,
                idimportacion = viewModel.IdImportacion ?? 0,
                idempresa = viewModel.IdEmpresa,
                tipotransaccion = viewModel.TipoTransaccion ?? 1,
                cantidadrequerida = viewModel.CantidadRequerida,
                cantidadcamiones = viewModel.CantidadCamiones,
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
                    // Log validation errors
                    foreach (var modelState in ModelState.Values)
                    {
                        foreach (var error in modelState.Errors)
                        {
                            _logger.LogError($"Validation error: {error.ErrorMessage}");
                        }
                    }

                    // Reloading data for the view
                    ViewBag.Empresas = new SelectList(await _movimientoService.GetEmpresasSelectListAsync(), "Value", "Text", viewModel.IdEmpresa);
                    ViewBag.SelectedBarco = selectedBarco;
                    return View(viewModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating movimiento ID {id}: {ex.Message}");
                ModelState.AddModelError("", $"Ha ocurrido un error al guardar los cambios: {ex.Message}");

                // Reloading data for the view
                ViewBag.Empresas = new SelectList(await _movimientoService.GetEmpresasSelectListAsync(), "Value", "Text", viewModel.IdEmpresa);
                ViewBag.SelectedBarco = selectedBarco;
                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id, int? selectedBarco)
        {
            if (id == null)
                return NotFound();

            var viewModel = await _movimientoService.GetRegistroRequerimientoByIdAsync(id.Value);
            if (viewModel == null)
                return NotFound();

            ViewBag.SelectedBarco = selectedBarco;
            return View(viewModel);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, int? selectedBarco)
        {
            try
            {
                await _movimientoService.DeleteAsync(id);
                return RedirectToAction(nameof(Index), new { selectedBarco });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting movimiento ID {Id}: {Message}", id, ex.Message);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}


