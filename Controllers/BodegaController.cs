using Microsoft.AspNetCore.Mvc;
using API.Models;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;

namespace Sistema_de_Gestion_de_Importaciones.Controllers
{
    public class BodegaController : Controller
    {
        private readonly IBodegaService _bodegaService;
        private readonly ILogger<BodegaController> _logger;

        public BodegaController(IBodegaService bodegaService, ILogger<BodegaController> logger)
        {
            _bodegaService = bodegaService;
            _logger = logger;
        }

        // GET: Show list of bodegas
        public async Task<IActionResult> Index()
        {
            try
            {
                var bodegas = await _bodegaService.GetAllAsync();
                return View(bodegas);
            }
            catch (Exception)
            {
                //_logger.LogError(ex, "Error al obtener las bodegas");
                ViewBag.Error = "Error al cargar las bodegas. Por favor, intente más tarde.";
                return View(new List<Empresa_Bodegas>());
            }
        }

        // GET: Show the create form for a new bodega
        public IActionResult Crear()
        {
            return View(new Empresa_Bodegas());
        }

        // POST: Create a new bodega using the service
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Empresa_Bodegas model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                if (string.IsNullOrWhiteSpace(model.bodega))
                {
                    ModelState.AddModelError("bodega", "El nombre de la bodega es requerido");
                    return View(model);
                }

                await _bodegaService.CreateAsync(model);
                //_logger.LogInformation($"Bodega '{model.Nombre}' creada exitosamente.");
                TempData["Success"] = "Bodega creada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                //_logger.LogError(ex, $"Error al crear la bodega '{model.Nombre}'.");
                ModelState.AddModelError("", "Ocurrió un error al crear la bodega.");
                return View(model);
            }
        }

        // GET: Muestra el formulario para editar una bodega
        public async Task<IActionResult> Editar(int id)
        {
            var bodega = await _bodegaService.GetByIdAsync(id);
            if (bodega == null)
            {
                return NotFound();
            }

            return View(bodega);
        }

        // POST: Edita una bodega consumiendo el service
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Empresa_Bodegas model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var bodegaExistente = await _bodegaService.GetByIdAsync(model.id);
                if (bodegaExistente == null)
                {
                    return NotFound();
                }

                await _bodegaService.UpdateAsync(model.id, model);
                //_logger.LogInformation($"Bodega '{model.Nombre}' actualizada exitosamente.");
                TempData["Success"] = "Bodega actualizada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                //_logger.LogError(ex, $"Error al actualizar la bodega '{model.Nombre}'.");
                ModelState.AddModelError("", "Ocurrió un error al actualizar la bodega.");
                return View(model);
            }
        }

        // GET: Muestra el formulario para eliminar una bodega
        public async Task<IActionResult> Eliminar(int id)
        {
            var bodega = await _bodegaService.GetByIdAsync(id);
            if (bodega == null)
            {
                return NotFound();
            }

            return View(bodega);
        }

        // POST: Elimina una bodega consumiendo el service
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarConfirmado(int id)
        {
            try
            {
                var bodega = await _bodegaService.GetByIdAsync(id);
                if (bodega == null)
                {
                    //_logger.LogWarning($"Intento de eliminar bodega inexistente con ID: {id}");
                    return NotFound();
                }

                await _bodegaService.DeleteAsync(id);
                //_logger.LogInformation($"Bodega '{bodega.Nombre}' eliminada exitosamente.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                //_logger.LogError(ex, $"Error al eliminar la bodega con ID: {id}");
                TempData["Error"] = "Ocurrió un error al eliminar la bodega.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}