using Microsoft.AspNetCore.Mvc;
using API.Models;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using Sistema_de_Gestion_de_Importaciones.Helpers;
using Microsoft.AspNetCore.Authorization;
using Sistema_de_Gestion_de_Importaciones.Extensions;

namespace Sistema_de_Gestion_de_Importaciones.Controllers
{
    [Authorize]
    public class BodegaController : Controller
    {
        private readonly IBodegaService _bodegaService;
        private readonly ILogger<BodegaController> _logger;

        public BodegaController(IBodegaService bodegaService, ILogger<BodegaController> logger)
        {
            _bodegaService = bodegaService;
            _logger = logger;
        }

        [Authorize(Roles = "Administrador,Operador")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var bodegas = await _bodegaService.GetAllAsync();
                return View(bodegas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar las bodegas");
                this.Error("Error al cargar las bodegas. Por favor, intente más tarde.");
                return View(new List<Empresa_Bodegas>());
            }
        }

        [Authorize(Roles = "Administrador,Operador")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Operador")]
        public async Task<IActionResult> Create(Empresa_Bodegas nuevaBodega)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    nuevaBodega.idusuario = User.GetUserId();
                    await _bodegaService.CreateAsync(nuevaBodega);
                    this.Success("Bodega creada correctamente.");
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

        [Authorize(Roles = "Administrador,Operador")]
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
                this.Error("Ocurrió un error al obtener la bodega.");
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Operador")]
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
                    updatedBodega.idusuario = User.GetUserId();
                    await _bodegaService.UpdateAsync(id, updatedBodega);
                    this.Success("Bodega actualizada correctamente.");
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

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int id)
        {
            var bodega = await _bodegaService.GetByIdAsync(id);
            if (bodega == null)
            {
                return NotFound();
            }
            return View(bodega);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _bodegaService.DeleteAsync(id);
                this.Success("Bodega eliminada correctamente.");
                return RedirectToAction("Index", "Bodega");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar la bodega con ID: {id}");
                var bodega = await _bodegaService.GetByIdAsync(id);
                this.Error($"Error al eliminar la bodega {bodega?.bodega}. Por favor, intente más tarde.");
                return View(bodega);
            }
        }
    }
}