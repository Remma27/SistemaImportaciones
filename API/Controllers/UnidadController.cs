using Microsoft.AspNetCore.Mvc;
using API.Models;
using API.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UnidadController : ControllerBase
    {
        private readonly ApiContext _context;
        private readonly ILogger<UnidadController> _logger;

        public UnidadController(ApiContext context, ILogger<UnidadController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Unidad unidad)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (unidad.id != 0)
                {
                    _logger.LogWarning("Intento de crear unidad con ID no cero: {Id}", unidad.id);
                    return BadRequest(new { error = "El ID debe ser 0 para crear una nueva unidad" });
                }

                await _context.Unidades.AddAsync(unidad);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Unidad creada exitosamente: {Id}", unidad.id);
                return CreatedAtAction(nameof(Get), new { id = unidad.id }, unidad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear unidad");
                return StatusCode(500, new { error = "Error interno del servidor al crear la unidad" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Unidad unidad)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (id != unidad.id)
                {
                    _logger.LogWarning("ID de ruta {RouteId} no coincide con ID del cuerpo {BodyId}", id, unidad.id);
                    return BadRequest(new { error = "ID de ruta no coincide con ID del cuerpo" });
                }

                var unidadExistente = await _context.Unidades.FindAsync(id);
                if (unidadExistente == null)
                {
                    _logger.LogWarning("Unidad no encontrada: {Id}", id);
                    return NotFound(new { error = $"Unidad con ID {id} no encontrada" });
                }

                _context.Entry(unidadExistente).CurrentValues.SetValues(unidad);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Unidad actualizada exitosamente: {Id}", id);
                return Ok(unidad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar unidad con ID: {Id}", id);
                return StatusCode(500, new { error = "Error interno del servidor al actualizar la unidad" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var unidad = await _context.Unidades.FindAsync(id);
                if (unidad == null)
                {
                    _logger.LogWarning("Unidad no encontrada: {Id}", id);
                    return NotFound(new { error = $"Unidad con ID {id} no encontrada" });
                }

                return Ok(unidad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener unidad con ID: {Id}", id);
                return StatusCode(500, new { error = "Error interno del servidor al obtener la unidad" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var unidad = await _context.Unidades.FindAsync(id);
                if (unidad == null)
                {
                    _logger.LogWarning("Intento de eliminar unidad inexistente: {Id}", id);
                    return NotFound(new { error = $"Unidad con ID {id} no encontrada" });
                }

                _context.Unidades.Remove(unidad);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Unidad eliminada exitosamente: {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar unidad con ID: {Id}", id);
                return StatusCode(500, new { error = "Error interno del servidor al eliminar la unidad" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var unidades = await _context.Unidades.ToListAsync();
                return Ok(unidades);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener lista de unidades");
                return StatusCode(500, new { error = "Error interno del servidor al obtener la lista de unidades" });
            }
        }
    }
}
