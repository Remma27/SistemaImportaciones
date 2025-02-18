using Microsoft.AspNetCore.Mvc;
using Sistema_de_Gestion_de_Importaciones.Models;
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

        // GET: Muestra la lista de movimientos
        public async Task<IActionResult> Index()
        {
            var movimientos = await _movimientoService.GetAllAsync();
            return View(movimientos);
        }

        // GET: Muestra el formulario para crear un nuevo movimiento
        public IActionResult Crear()
        {
            return View(new Movimiento());
        }

        // POST: Crea un nuevo movimiento consumiendo el service
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

        // GET: Muestra el formulario para editar un movimiento
        public async Task<IActionResult> Editar(int id)
        {
            var movimiento = await _movimientoService.GetByIdAsync(id);
            if (movimiento == null)
            {
                return NotFound();
            }

            return View(movimiento);
        }

        // POST: Edita un movimiento consumiendo el service
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

        // GET: Muestra el formulario para eliminar un movimiento
        public async Task<IActionResult> Eliminar(int id)
        {
            var movimiento = await _movimientoService.GetByIdAsync(id);
            if (movimiento == null)
            {
                return NotFound();
            }

            return View(movimiento);
        }

        // POST: Elimina un movimiento consumiendo el service
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