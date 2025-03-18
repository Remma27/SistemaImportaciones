using Microsoft.AspNetCore.Mvc;
using API.Models;
using API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class HistorialController : ControllerBase
    {
        private readonly ApiContext _context;
        private readonly ILogger<HistorialController> _logger;

        public HistorialController(ApiContext context, ILogger<HistorialController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                // Get all records without any filtering
                var registros = await _context.HistorialCambios
                    .Include(h => h.Usuario)
                    .OrderByDescending(h => h.FechaHora)
                    .Select(h => new
                    {
                        h.Id,
                        h.UsuarioId,
                        NombreUsuario = h.Usuario != null ? h.Usuario.nombre : "Sistema",
                        h.Tabla,
                        h.TipoOperacion,
                        h.FechaHora,
                        h.Descripcion,
                        h.DatosJSON
                    })
                    .ToListAsync();

                return Ok(new
                {
                    TotalRegistros = registros.Count,
                    Registros = registros
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener historial de cambios");
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var historial = await _context.HistorialCambios
                    .Include(h => h.Usuario)
                    .FirstOrDefaultAsync(h => h.Id == id);

                if (historial == null)
                    return NotFound(new { message = $"No se encontró el registro de historial con id {id}" });

                return Ok(new
                {
                    historial.Id,
                    historial.UsuarioId,
                    NombreUsuario = historial.Usuario != null ? historial.Usuario.nombre : "Sistema",
                    historial.Tabla,
                    historial.TipoOperacion,
                    historial.FechaHora,
                    historial.Descripcion,
                    historial.DatosJSON
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener historial con ID {id}");
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTablasDisponibles()
        {
            try
            {
                var tablas = await _context.HistorialCambios
                    .Select(h => h.Tabla)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToListAsync();

                return Ok(tablas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener lista de tablas disponibles");
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }
    }
}
