using Microsoft.AspNetCore.Mvc;
using API.Models;
using API.Data;
using Microsoft.AspNetCore.Authorization;
using API.Services;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "Administrador,Operador")]
    public class BodegaController : ControllerBase
    {
        private readonly ApiContext _context;
        private readonly HistorialService _historialService;
        private readonly ILogger<BodegaController> _logger;
        
        public BodegaController(ApiContext context, HistorialService historialService, ILogger<BodegaController> logger)
        {
            _context = context;
            _historialService = historialService;
            _logger = logger;
        }

        // Endpoint para crear una nueva Bodega
        [HttpPost]
        [Consumes("application/json")]
        public JsonResult Create([FromBody] Empresa_Bodegas bodega)
        {
            try
            {
                _logger.LogInformation("Iniciando creación de bodega");
                
                if (bodega.id != 0)
                {
                    return new JsonResult(BadRequest("El id debe ser 0 para crear una nueva bodega.")) { StatusCode = 400 };
                }

                _context.Empresa_Bodegas.Add(bodega);
                _context.SaveChanges();
                
                _historialService.GuardarHistorial("CREAR", bodega, "Empresa_Bodegas", $"Creación de bodega ID: {bodega.id}");
                _logger.LogInformation($"Bodega creada con ID: {bodega.id}, registro en historial completado");
                
                return new JsonResult(bodega);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear bodega");
                return new JsonResult(new { message = "Error al crear bodega", error = ex.Message }) { StatusCode = 500 };
            }
        }

        // Endpoint para editar una Bodega existente
        [HttpPut]
        public JsonResult Edit(Empresa_Bodegas bodega)
        {
            try
            {
                _logger.LogInformation($"Iniciando edición de bodega ID: {bodega.id}");
                
                var bodegaInDb = _context.Empresa_Bodegas.Find(bodega.id);
                if (bodegaInDb == null)
                {
                    return new JsonResult(NotFound());
                }
                
                _historialService.GuardarHistorial(
                    "ANTES_EDITAR", 
                    bodegaInDb, 
                    "Empresa_Bodegas", 
                    $"Estado anterior de bodega ID: {bodegaInDb.id}, Nombre: {bodegaInDb.bodega}"
                );

                _context.Entry(bodegaInDb).CurrentValues.SetValues(bodega);
                _context.SaveChanges();
                
                _historialService.GuardarHistorial(
                    "DESPUES_EDITAR", 
                    bodega, 
                    "Empresa_Bodegas", 
                    $"Estado nuevo de bodega ID: {bodega.id}, Nombre: {bodega.bodega}"
                );
                
                return new JsonResult(Ok(bodega));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar bodega: {Id}", bodega.id);
                return new JsonResult(StatusCode(500, new { message = "Error al editar bodega", error = ex.Message }));
            }
        }

        // Get
        [HttpGet]
        public JsonResult Get(int id)
        {
            var result = _context.Empresa_Bodegas.Find(id);
            if (result == null)
            {
                return new JsonResult(NotFound()) { StatusCode = 404 };
            }
            return new JsonResult(result);
        }

        // Delete
        [HttpDelete]
        public JsonResult Delete(int id)
        {
            try
            {
                _logger.LogInformation($"Iniciando eliminación de bodega ID: {id}");
                
                var result = _context.Empresa_Bodegas.Find(id);
                if (result == null)
                {
                    return new JsonResult(NotFound()) { StatusCode = 404 };
                }
                
                _historialService.GuardarHistorial("ELIMINAR", result, "Empresa_Bodegas", $"Eliminación de bodega ID: {id}");
                
                _context.Empresa_Bodegas.Remove(result);
                _context.SaveChanges();
                
                _logger.LogInformation($"Bodega ID: {id} eliminada exitosamente");
                
                return new JsonResult(null) { StatusCode = 204 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar bodega: {Id}", id);
                return new JsonResult(new { message = "Error al eliminar bodega", error = ex.Message }) { StatusCode = 500 };
            }
        }

        // GetAll
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var result = await _context.Empresa_Bodegas.ToListAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener bodegas");
                return StatusCode(500, new { message = "Error al obtener bodegas", error = ex.Message });
            }
        }
    }
}
