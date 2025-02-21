using Microsoft.AspNetCore.Mvc;
using API.Models;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using Sistema_de_Gestion_de_Importaciones.Helpers;

namespace Sistema_de_Gestion_de_Importaciones.Controllers
{
    public class BarcoController : Controller
    {
        private readonly IBarcoService _barcoService;
        private readonly ILogger<BarcoController> _logger;

        public BarcoController(IBarcoService barcoService, ILogger<BarcoController> logger)
        {
            _barcoService = barcoService;
            _logger = logger;
        }

        // Muestra la lista de barcos
        public async Task<IActionResult> Index()
        {
            try
            {
                var barcos = await _barcoService.GetAllAsync();
                return View(barcos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los barcos");
                ViewBag.Error = "Error al cargar los barcos. Por favor, intente más tarde.";
                return View(new List<Barco>());
            }
        }

        // GET: Muestra el formulario para crear un nuevo barco
        public IActionResult Create()
        {
            return View();
        }

        // POST: Crea un nuevo barco consumiendo el service
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Barco barco)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    barco.idusuario = User.GetUserId(); // Asignar el ID del usuario autenticado
                    await _barcoService.CreateAsync(barco);
                    TempData["Success"] = "Barco creado correctamente.";
                    return RedirectToAction("Index", "Barco");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al crear el barco");
                    ModelState.AddModelError("", "Ocurrió un error al crear el barco.");
                }
            }
            return View(barco);
        }

        // GET: Muestra el formulario para editar un barco
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var barco = await _barcoService.GetByIdAsync(id);
                if (barco == null)
                {
                    return NotFound();
                }
                return View(barco);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener el barco con ID: {id}");
                TempData["Error"] = "Ocurrió un error al obtener el barco.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Edita un barco consumiendo el service
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Barco barco)
        {
            if (id != barco.id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    barco.idusuario = User.GetUserId(); // Asignar el ID del usuario autenticado
                    await _barcoService.UpdateAsync(id, barco);
                    TempData["Success"] = "Barco actualizado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error al actualizar el barco con ID: {id}");
                    ModelState.AddModelError("", "Ocurrió un error al actualizar el barco.");
                }
            }
            return View(barco);
        }

        // GET: Muestra el formulario para eliminar un barco
        public async Task<IActionResult> Delete(int id)
        {
            var barco = await _barcoService.GetByIdAsync(id);
            if (barco == null)
            {
                return NotFound();
            }
            return View(barco);
        }

        // POST: Elimina un barco consumiendo el service
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _barcoService.DeleteAsync(id);
                TempData["Success"] = "Barco eliminado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar el barco con ID: {id}");
                TempData["Error"] = "Ocurrió un error al eliminar el barco.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}