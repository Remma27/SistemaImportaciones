using Microsoft.AspNetCore.Mvc;
using API.Models;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;

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
        public IActionResult Crear()
        {
            return View(new Barco());
        }

        // POST: Crea un nuevo barco consumiendo el service
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Barco model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                if (string.IsNullOrWhiteSpace(model.nombrebarco))
                {
                    ModelState.AddModelError("nombrebarco", "El nombre del barco es requerido");
                    return View(model);
                }

                await _barcoService.CreateAsync(model);
                _logger.LogInformation($"Barco '{model.nombrebarco}' creado exitosamente.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al crear el barco '{model.nombrebarco}'.");
                ModelState.AddModelError("", "Ocurrió un error al crear el barco.");
                return View(model);
            }
        }

        // GET: Muestra el formulario para editar un barco
        public async Task<IActionResult> Editar(int id)
        {
            var barco = await _barcoService.GetByIdAsync(id);
            if (barco == null)
            {
                return NotFound();
            }
            return View(barco);
        }

        // POST: Edita un barco consumiendo el service
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Barco model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _barcoService.UpdateAsync(model.id, model);
                _logger.LogInformation("Barco actualizado exitosamente.");
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el barco.");
                ModelState.AddModelError("", "Ocurrió un error al actualizar el barco.");
                return View(model);
            }
        }

        // GET: Muestra el formulario para eliminar un barco
        public async Task<IActionResult> Eliminar(int id)
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
        public async Task<IActionResult> EliminarConfirmado(int id)
        {
            try
            {
                var barco = await _barcoService.GetByIdAsync(id);
                if (barco == null)
                {
                    _logger.LogWarning($"Intento de eliminar barco inexistente con ID: {id}");
                    return NotFound();
                }

                await _barcoService.DeleteAsync(id);
                _logger.LogInformation($"Barco '{barco.nombrebarco}' eliminado exitosamente.");
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