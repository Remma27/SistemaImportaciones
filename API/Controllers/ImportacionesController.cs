using Microsoft.AspNetCore.Mvc;
using API.Models;
using API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using API.Services;

namespace API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class ImportacionesController : ControllerBase
    {
        private readonly ApiContext _context;
        private readonly HistorialService _historialService;
        private readonly ILogger<ImportacionesController> _logger;
        
        public ImportacionesController(ApiContext context, HistorialService historialService, ILogger<ImportacionesController> logger)
        {
            _context = context;
            _historialService = historialService;
            _logger = logger;
        }

        [HttpPost]
        [Consumes("application/json")]
        [Authorize(Roles = "Administrador,Operador")]
        public JsonResult Create([FromBody] Importacion importacion)
        {
            try
            {
                if (importacion.id != 0)
                {
                    return new JsonResult(BadRequest("El id debe ser 0 para crear una nueva importacion."));
                }

                if (importacion.idbarco == 0)
                {
                    return new JsonResult(BadRequest("El barco es requerido."));
                }

                importacion.fechahorasystema = DateTime.Now;

                _context.Importaciones.Add(importacion);
                _context.SaveChanges();
                
                _historialService.GuardarHistorial("CREAR", importacion, "Importaciones", $"Creación: {importacion.id}");
                
                _logger.LogInformation($"Importación creada exitosamente, ID: {importacion.id}");
                return new JsonResult(Ok(importacion));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear importación");
                return new JsonResult(StatusCode(500, new { message = "Error al crear importación", error = ex.Message }));
            }
        }

        // Endpoint para editar una Importacion existente
        [HttpPut]
        [Authorize(Roles = "Administrador,Operador")]
        public JsonResult Edit(Importacion importacion)
        {
            try
            {
                _logger.LogInformation($"Iniciando edición de importación ID: {importacion.id}");
                
                if (importacion.id == 0)
                {
                    return new JsonResult(BadRequest("Debe proporcionar un id válido para editar una importacion."));
                }
                
                var importacionInDb = _context.Importaciones.Find(importacion.id);
                if (importacionInDb == null)
                {
                    return new JsonResult(NotFound());
                }
                
                _historialService.GuardarHistorial(
                    "ANTES_EDITAR", 
                    importacionInDb, 
                    "Importaciones", 
                    $"Estado anterior de importación ID: {importacionInDb.id}"
                );
                
                if (importacion.fechahorasystema == null)
                {
                    importacion.fechahorasystema = importacionInDb.fechahorasystema;
                }
                
                _context.Entry(importacionInDb).CurrentValues.SetValues(importacion);
                _context.SaveChanges();
                
                _historialService.GuardarHistorial(
                    "DESPUES_EDITAR", 
                    importacion, 
                    "Importaciones", 
                    $"Estado nuevo de importación ID: {importacion.id}"
                );
                
                return new JsonResult(Ok(importacion));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar importación: {Id}", importacion.id);
                return new JsonResult(StatusCode(500, new { message = "Error al editar importación", error = ex.Message }));
            }
        }

        // Get
        [HttpGet]
        public JsonResult Get(int id)
        {
            var result = _context.Importaciones
                .Include(i => i.Barco)
                .FirstOrDefault(i => i.id == id);
            if (result == null)
            {
                return new JsonResult(NotFound());
            }
            return new JsonResult(Ok(result));
        }

        // Delete
        [HttpDelete]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var importacion = await _context.Importaciones.FindAsync(id);
                if (importacion == null)
                {
                    return NotFound(new { message = "Importación no encontrada" });
                }

                var tieneMovimientos = await _context.Movimientos
                    .AnyAsync(m => m.idimportacion == id);
                
                if (tieneMovimientos)
                {
                    return BadRequest(new {
                        message = "No se puede eliminar esta importación porque tiene movimientos asociados."
                    });
                }

                _historialService.GuardarHistorial("ELIMINAR", importacion, "Importaciones", $"Eliminación de importación {importacion.id}");
                
                _context.Importaciones.Remove(importacion);
                await _context.SaveChangesAsync();
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar importación: {Id}", id);
                return StatusCode(500, new { message = "Error al eliminar importación", error = ex.Message });
            }
        }

        // Endpoint para obtener todas las importaciones
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var result = await _context.Importaciones
                    .Include(i => i.Barco)
                    .ToListAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener importaciones");
                return StatusCode(500, new { message = "Error al obtener importaciones", error = ex.Message });
            }
        }
    }
}
