using Microsoft.AspNetCore.Mvc;
using API.Models;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;

namespace Sistema_de_Gestion_de_Importaciones.Controllers
{
    public class MovimientoController : Controller
    {
        private readonly IMovimientoService _movimientoService;
        private readonly ILogger<MovimientoController> _logger;

        public MovimientoController(IMovimientoService movimientoService, ILogger<MovimientoController> logger)
        {
            _movimientoService = movimientoService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var movimientos = await _movimientoService.GetAllAsync();
            return View(movimientos);
        }

        public IActionResult Crear()
        {
            return View(new Movimiento());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Movimiento model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _movimientoService.CreateAsync(model);
                _logger.LogInformation($"Movimiento con ID '{model.id}' creado exitosamente.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al crear el movimiento con ID '{model.id}'.");
                ModelState.AddModelError("", "Ocurrió un error al crear el movimiento.");
                return View(model);
            }
        }

        public async Task<IActionResult> Editar(int id)
        {
            var movimiento = await _movimientoService.GetByIdAsync(id);
            if (movimiento == null)
            {
                return NotFound();
            }

            return View(movimiento);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Movimiento model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var movimientoExistente = await _movimientoService.GetByIdAsync(model.id);
                if (movimientoExistente == null)
                {
                    return NotFound();
                }

                await _movimientoService.UpdateAsync(model.id, model);
                _logger.LogInformation($"Movimiento con ID '{model.id}' actualizado exitosamente.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar el movimiento con ID '{model.id}'.");
                ModelState.AddModelError("", "Ocurrió un error al actualizar el movimiento.");
                return View(model);
            }
        }

        public async Task<IActionResult> Eliminar(int id)
        {
            var movimiento = await _movimientoService.GetByIdAsync(id);
            if (movimiento == null)
            {
                return NotFound();
            }

            return View(movimiento);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarConfirmado(int id)
        {
            try
            {
                var movimiento = await _movimientoService.GetByIdAsync(id);
                if (movimiento == null)
                {
                    return NotFound();
                }

                await _movimientoService.DeleteAsync(id);
                _logger.LogInformation($"Movimiento con ID '{id}' eliminado exitosamente.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar el movimiento con ID '{id}'.");
                ModelState.AddModelError("", "Ocurrió un error al eliminar el movimiento.");
                return View(new Movimiento { id = id });
            }
        }
    }
}