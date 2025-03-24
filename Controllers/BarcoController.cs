using Microsoft.AspNetCore.Mvc;
using API.Models;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using Sistema_de_Gestion_de_Importaciones.Helpers;
using Microsoft.AspNetCore.Authorization;
using Sistema_de_Gestion_de_Importaciones.Extensions;

namespace Sistema_de_Gestion_de_Importaciones.Controllers
{
    [Authorize]
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
        [Authorize(Roles = "Administrador,Operador")]
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
                this.Error("Error al cargar los barcos. Por favor, intente más tarde.");
                return View(new List<Barco>());
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
        public async Task<IActionResult> Create(Barco barco)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    barco.idusuario = User.GetUserId();
                    await _barcoService.CreateAsync(barco);
                    this.Success("Barco creado correctamente.");
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

        [Authorize(Roles = "Administrador,Operador")]
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
                this.Error("Ocurrió un error al obtener el barco.");
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Operador")]
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
                    barco.idusuario = User.GetUserId();
                    await _barcoService.UpdateAsync(id, barco);
                    this.Success("Barco actualizado correctamente.");
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

        [Authorize(Roles = "Administrador,Operador")]
        public async Task<IActionResult> Delete(int id)
        {
            var barco = await _barcoService.GetByIdAsync(id);
            if (barco == null)
            {
                return NotFound();
            }
            return View(barco);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        [Authorize(Roles = "Administrador,Operador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _barcoService.DeleteAsync(id);
                this.Success("Barco eliminado correctamente.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar el barco con ID: {id}");
                this.Error("Ocurrió un error al eliminar el barco.");
                return RedirectToAction(nameof(Index));
            }
        }
    }
}