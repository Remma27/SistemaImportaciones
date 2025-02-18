using Microsoft.AspNetCore.Mvc;
using Sistema_de_Gestion_de_Importaciones.Models;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;

namespace Sistema_de_Gestion_de_Importaciones.Controllers
{
    public class ImportacionController : Controller
    {
        private readonly IImportacionService _importacionService;
        private readonly ILogger<ImportacionController> _logger;

        public ImportacionController(IImportacionService importacionService, ILogger<ImportacionController> logger)
        {
            _importacionService = importacionService;
            _logger = logger;
        }

        // GET: Show list of importaciones
        public async Task<IActionResult> Index()
        {
            var importaciones = await _importacionService.GetAllAsync();
            return View(importaciones);
        }

        // GET: Show the create form for a new importacion
        public IActionResult Crear()
        {
            return View(new Importacion());
        }

        // POST: Create a new importacion using the service
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Importacion model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _importacionService.CreateAsync(model);
                TempData["Success"] = "Importacion creada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Ocurrió un error al crear la importacion.");
                return View(model);
            }
        }

        // GET: Show the edit form for an importacion
        public async Task<IActionResult> Editar(int id)
        {
            var importacion = await _importacionService.GetByIdAsync(id);
            if (importacion == null)
            {
                return NotFound();
            }

            return View(importacion);
        }

        // POST: Edit an importacion using the service
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Importacion model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Se omite la validación de "nombre", ya que el modelo Importacion no la tiene.
                await _importacionService.UpdateAsync(model.id, model);
                TempData["Success"] = "Importacion actualizada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Ocurrió un error al actualizar la importacion.");
                return View(model);
            }
        }

        // GET: Show the delete confirmation page for an importacion
        public async Task<IActionResult> Eliminar(int id)
        {
            var importacion = await _importacionService.GetByIdAsync(id);
            if (importacion == null)
            {
                return NotFound();
            }

            return View(importacion);
        }

        // POST: Delete an importacion using the service
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(Importacion model)
        {
            try
            {
                var importacionExistente = await _importacionService.GetByIdAsync(model.id);
                if (importacionExistente == null)
                {
                    return NotFound();
                }

                await _importacionService.DeleteAsync(model.id);
                TempData["Success"] = "Importacion eliminada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Ocurrió un error al eliminar la importacion.");
                return View(model);
            }
        }
    }
}