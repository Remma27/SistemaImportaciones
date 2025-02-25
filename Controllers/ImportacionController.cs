using Microsoft.AspNetCore.Mvc;
using API.Models;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using Sistema_de_Gestion_de_Importaciones.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Sistema_de_Gestion_de_Importaciones.Controllers
{
    public class ImportacionController : Controller
    {
        private readonly IImportacionService _importacionService;
        private readonly IBarcoService _barcoService;
        private readonly ILogger<ImportacionController> _logger;

        public ImportacionController(
            IImportacionService importacionService,
            IBarcoService barcoService,
            ILogger<ImportacionController> logger)
        {
            _importacionService = importacionService;
            _barcoService = barcoService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var importaciones = await _importacionService.GetAllAsync();
            return View(importaciones);
        }

        public async Task<IActionResult> Create()
        {
            try
            {
                var barcos = await _barcoService.GetAllAsync();
                ViewBag.Barcos = new SelectList(barcos, "id", "nombrebarco");
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar los barcos para el formulario de creación");
                TempData["Error"] = "Error al cargar la lista de barcos.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Importacion importacion)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    importacion.idusuario = User.GetUserId();
                    await _importacionService.CreateAsync(importacion);
                    TempData["Success"] = "Importacion creada correctamente.";
                    return RedirectToAction("Index", "Importacion");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al crear la importacion");
                    ModelState.AddModelError("", "Ocurrió un error al crear la importacion.");
                }
            }
            return View(importacion);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var importacion = await _importacionService.GetByIdAsync(id);
                if (importacion == null)
                {
                    return NotFound();
                }

                var barcos = await _barcoService.GetAllAsync();

                ViewBag.Barcos = new SelectList(
                    barcos,
                    "id",
                    "nombrebarco",
                    importacion.idbarco
                );

                return View(importacion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar la importación para editar");
                TempData["Error"] = "Error al cargar los datos de la importación.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Importacion importacion)
        {
            if (id != importacion.id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    importacion.idusuario = User.GetUserId();
                    await _importacionService.UpdateAsync(id, importacion);
                    TempData["Success"] = "Importacion actualizada correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al actualizar la importacion");
                    ModelState.AddModelError("", "Ocurrió un error al actualizar la importacion.");
                }
            }
            return View(importacion);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var importacion = await _importacionService.GetByIdAsync(id);
            if (importacion == null)
            {
                return NotFound();
            }
            return View(importacion);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _importacionService.DeleteAsync(id);
                TempData["Success"] = "Importacion eliminada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar la empresa con ID {Id}", id);
                var importacion = await _importacionService.GetByIdAsync(id);
                ViewBag.Error = "Ocurrió un error al eliminar la importacion: " + ex.Message;
                return View("Delete", importacion);
            }
        }
    }
}