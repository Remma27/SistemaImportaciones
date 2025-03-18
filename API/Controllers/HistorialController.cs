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
                _logger.LogInformation("Obteniendo todos los registros de historial sin filtrado");
                
                // Verificar cuántos registros totales hay en la base de datos
                var totalRegistrosEnBD = await _context.HistorialCambios.CountAsync();
                _logger.LogInformation($"Total registros en BD: {totalRegistrosEnBD}");
                
                // Obtener TODOS los registros directamente sin filtros
                var registros = await _context.HistorialCambios
                    .AsNoTracking()
                    .OrderByDescending(h => h.FechaHora)
                    .ToListAsync();

                _logger.LogInformation($"Registros recuperados de la base de datos: {registros.Count}");
                
                // Obtener todos los usuarios válidos
                var usuarios = await _context.Usuarios
                    .AsNoTracking()
                    .Select(u => new { u.id, u.nombre })
                    .ToDictionaryAsync(u => u.id, u => u.nombre);
                
                // Mapear los resultados
                var resultadoFinal = registros.Select(h => new
                {
                    h.Id,
                    h.UsuarioId,
                    NombreUsuario = usuarios.ContainsKey(h.UsuarioId) ? usuarios[h.UsuarioId] : "Sistema",
                    h.Tabla,
                    h.TipoOperacion,
                    h.FechaHora,
                    h.Descripcion,
                    h.DatosJSON
                }).ToList();

                _logger.LogInformation($"Total de registros obtenidos: {resultadoFinal.Count}");
                
                // Verificar explícitamente si hay registros con UsuarioId = 1 en el resultado final
                var conteoUsuario1EnResultado = resultadoFinal.Count(r => r.UsuarioId == 1);
                _logger.LogInformation($"Registros con UsuarioId = 1 en resultado: {conteoUsuario1EnResultado}");

                return Ok(new
                {
                    TotalRegistrosEnBD = totalRegistrosEnBD,
                    TotalRegistrosObtenidos = resultadoFinal.Count,
                    RegistrosUsuario1 = conteoUsuario1EnResultado,
                    Registros = resultadoFinal
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
                _logger.LogInformation($"Obteniendo registro de historial con ID: {id}");
                
                // Obtener el registro de historial por ID sin usar Include
                var historial = await _context.HistorialCambios
                    .AsNoTracking()
                    .FirstOrDefaultAsync(h => h.Id == id);

                if (historial == null)
                    return NotFound(new { message = $"No se encontró el registro de historial con id {id}" });
                    
                // Obtener el nombre del usuario desde la tabla Usuarios
                string nombreUsuario = "Sistema";
                if (historial.UsuarioId > 0)
                {
                    var usuario = await _context.Usuarios
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.id == historial.UsuarioId);
                        
                    if (usuario != null)
                    {
                        nombreUsuario = usuario.nombre ?? "Sistema";
                    }
                }

                _logger.LogInformation($"Registro encontrado. UsuarioId: {historial.UsuarioId}, Nombre: {nombreUsuario}");
                
                return Ok(new
                {
                    historial.Id,
                    historial.UsuarioId,
                    NombreUsuario = nombreUsuario,
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
