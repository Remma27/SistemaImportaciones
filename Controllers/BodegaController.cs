using Microsoft.AspNetCore.Mvc;
using API.Models;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using Sistema_de_Gestion_de_Importaciones.Helpers;

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
        public IActionResult Create()
        {
            return View();
        }

        // POST: Create a new bodega using the service
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Empresa_Bodegas nuevaBodega)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    nuevaBodega.idusuario = User.GetUserId(); // Assign the ID of the authenticated user
                    await _bodegaService.CreateAsync(nuevaBodega);
                    TempData["Success"] = "Bodega creada correctamente.";
                    return RedirectToAction("Index", "Bodega");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al crear la bodega");
                    ModelState.AddModelError("", "Ocurrió un error al crear la bodega.");
                }
            }
            return View(nuevaBodega);
        }

        // GET: Muestra el formulario para editar una bodega
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var bodega = await _bodegaService.GetByIdAsync(id);
                if (bodega == null)
                {
                    return NotFound();
                }

                return View(bodega);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener la bodega con ID {id}");
                TempData["Error"] = "Ocurrió un error al obtener la bodega.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Edita una bodega consumiendo el service
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Empresa_Bodegas updatedBodega)
        {
            if (id != updatedBodega.id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    updatedBodega.idusuario = User.GetUserId(); // Asignar el ID del usuario autenticado
                    await _bodegaService.UpdateAsync(id, updatedBodega);
                    TempData["Success"] = "Bodega actualizada correctamente.";
                    return RedirectToAction("Index", "Bodega");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error al actualizar la bodega con ID: {id}");
                    ModelState.AddModelError("", "Ocurrió un error al actualizar la bodega.");
                }
            }
            return View(updatedBodega);
        }

        // GET: Muestra el formulario para eliminar una bodega
        public async Task<IActionResult> Delete(int id)
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
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _bodegaService.DeleteAsync(id);
                TempData["Success"] = "Bodega eliminada correctamente.";
                return RedirectToAction("Index", "Bodega");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar la bodega con ID: {id}");
                var bodega = await _bodegaService.GetByIdAsync(id);
                ViewBag.Error = $"Error al eliminar la bodega {bodega?.bodega}. Por favor, intente más tarde.";
                return View(bodega);
            }
        }
    }
}