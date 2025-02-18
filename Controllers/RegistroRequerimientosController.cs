using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sistema_de_Gestion_de_Importaciones.ViewModels;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using Sistema_de_Gestion_de_Importaciones.Helpers; // Para GetUserId()
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sistema_de_Gestion_de_Importaciones.Models;

namespace Sistema_de_Gestion_de_Importaciones.Controllers
{
    [Authorize]
    public class RegistroRequerimientosController : Controller
    {
        private readonly IRegistroRequerimientosService _registroService;
        private readonly ILogger<RegistroRequerimientosController> _logger;

        public RegistroRequerimientosController(IRegistroRequerimientosService registroService,
                                                ILogger<RegistroRequerimientosController> logger)
        {
            _registroService = registroService;
            _logger = logger;
        }

        // GET: RegistroRequerimientos
        public async Task<IActionResult> Index(int? selectedBarco)
        {
            try
            {
                _logger.LogInformation("Starting data retrieval for RegistroRequerimientos");

                // Obtener el select list de barcos a través del service
                var barcosSelect = await _registroService.GetBarcosSelectListAsync();
                ViewBag.Barcos = new SelectList(barcosSelect, "Value", "Text", selectedBarco);

                // Si no se ha seleccionado un barco, retornar un listado vacío
                if (!selectedBarco.HasValue)
                {
                    return View(new List<RegistroRequerimientosViewModel>());
                }

                var result = await _registroService.GetRegistroRequerimientosAsync(selectedBarco.Value);
                _logger.LogInformation($"Data retrieval successful, returning view with {result?.Count ?? 0} items");
                return View(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading registro requerimientos: {Message}", ex.Message);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: RegistroRequerimientos/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                ViewBag.Importaciones = new SelectList(await _registroService.GetImportacionesSelectListAsync(), "Value", "Text");
                ViewBag.Empresas = new SelectList(await _registroService.GetEmpresasSelectListAsync(), "Value", "Text");

                var viewModel = new RegistroRequerimientosViewModel
                {
                    FechaHora = DateTime.Now
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create form for registro requerimientos: {Message}", ex.Message);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // POST: RegistroRequerimientos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("fechahora,idimportacion,idempresa,tipotransaccion,cantidadrequerida,cantidadcamiones")] RegistroRequerimientosViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var userId = HttpContext.User.GetUserId().ToString();
                    var movimiento = new Movimiento
                    {
                        fechahora = viewModel.FechaHora,
                        idimportacion = viewModel.IdImportacion,
                        idempresa = viewModel.IdEmpresa,
                        tipotransaccion = viewModel.TipoTransaccion,
                        cantidadrequerida = (decimal?)viewModel.CantidadRequerida,
                        cantidadcamiones = viewModel.CantidadCamiones
                    };
                    await _registroService.CreateAsync(movimiento);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating registro requerimientos: {Message}", ex.Message);
                    return StatusCode(500, $"Internal server error: {ex.Message}");
                }
            }

            // Recargar los select lists en caso de error
            ViewBag.Importaciones = new SelectList(await _registroService.GetImportacionesSelectListAsync(), "Value", "Text", viewModel.IdImportacion);
            ViewBag.Empresas = new SelectList(await _registroService.GetEmpresasSelectListAsync(), "Value", "Text", viewModel.IdEmpresa);
            return View(viewModel);
        }

        // GET: RegistroRequerimientos/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            try
            {
                var viewModel = await _registroService.GetRegistroRequerimientoByIdAsync(id.Value);
                if (viewModel == null)
                    return NotFound();

                ViewBag.Importaciones = new SelectList(await _registroService.GetImportacionesSelectListAsync(), "Value", "Text", viewModel.IdImportacion);
                ViewBag.Empresas = new SelectList(await _registroService.GetEmpresasSelectListAsync(), "Value", "Text", viewModel.IdEmpresa);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit form for movimiento ID {Id}: {Message}", id, ex.Message);
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: RegistroRequerimientos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("[controller]/Edit/{id}")]
        public async Task<IActionResult> Edit(int id, [Bind("IdMovimiento,FechaHora,IdImportacion,IdEmpresa,TipoTransaccion,CantidadRequerida,CantidadCamiones")] RegistroRequerimientosViewModel viewModel)
        {
            if (id != viewModel.IdMovimiento)
            {
                _logger.LogWarning("ID mismatch in Edit Post. URL ID: {UrlId}, Model ID: {ModelId}", id, viewModel.IdMovimiento);
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var userId = HttpContext.User.GetUserId().ToString();
                    await _registroService.UpdateAsync(id, viewModel, userId);
                    _logger.LogInformation("Successfully updated movimiento ID: {Id}", id);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating movimiento ID {Id}: {Message}", id, ex.Message);
                    ModelState.AddModelError("", "Ha ocurrido un error al guardar los cambios.");
                }
            }

            try
            {
                ViewBag.Importaciones = new SelectList(await _registroService.GetImportacionesSelectListAsync(), "Value", "Text", viewModel.IdImportacion);
                ViewBag.Empresas = new SelectList(await _registroService.GetEmpresasSelectListAsync(), "Value", "Text", viewModel.IdEmpresa);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading edit form data: {Message}", ex.Message);
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: RegistroRequerimientos/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var viewModel = await _registroService.GetRegistroRequerimientoByIdAsync(id.Value);
            if (viewModel == null)
                return NotFound();

            return View(viewModel);
        }

        // POST: RegistroRequerimientos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _registroService.DeleteAsync(id);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting movimiento ID {Id}: {Message}", id, ex.Message);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}


