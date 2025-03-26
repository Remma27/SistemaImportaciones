using Microsoft.AspNetCore.Mvc;
using API.Models;
using API.Data;
using Microsoft.AspNetCore.Authorization;
using API.Services;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "Administrador,Operador")]
    public class EmpresaController : ControllerBase
    {
        private readonly ApiContext _context;
        private readonly HistorialService _historialService;
        private readonly ILogger<EmpresaController> _logger;

        public EmpresaController(ApiContext context, HistorialService historialService, ILogger<EmpresaController> logger)
        {
            _context = context;
            _historialService = historialService;
            _logger = logger;
        }

        // Endpoint para crear una nueva Empresa
        [HttpPost]
        [Consumes("application/json")]
        public JsonResult Create([FromBody] Empresa empresa)
        {
            try
            {
                _logger.LogInformation($"Iniciando creación de empresa");

                if (empresa.id_empresa != 0)
                {
                    return new JsonResult(BadRequest("El id debe ser 0 para crear una nueva empresa."));
                }

                empresa.nombreempresa = empresa.nombreempresa?.ToUpper();

                _context.Empresas.Add(empresa);
                _context.SaveChanges();

                _historialService.GuardarHistorial("CREAR", empresa, "Empresas", $"Creación: {empresa.nombreempresa}");
                _logger.LogInformation($"Empresa creada con ID: {empresa.id_empresa}, registro en historial completado");

                return new JsonResult(Ok(empresa));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear empresa");
                return new JsonResult(StatusCode(500, new { message = "Error al crear empresa", error = ex.Message }));
            }
        }

        // Endpoint para editar una Empresa existente
        [HttpPut]
        public JsonResult Edit(Empresa empresa)
        {
            try
            {
                _logger.LogInformation($"Iniciando edición de empresa ID: {empresa.id_empresa}");
                
                if (empresa.id_empresa == 0)
                {
                    return new JsonResult(BadRequest("Debe proporcionar un id válido para editar una empresa."));
                }
                
                var empresaInDb = _context.Empresas.Find(empresa.id_empresa);
                if (empresaInDb == null)
                {
                    return new JsonResult(NotFound());
                }
                
                _historialService.GuardarHistorial(
                    "ANTES_EDITAR", 
                    empresaInDb, 
                    "Empresas", 
                    $"Estado anterior de empresa {empresaInDb.nombreempresa} (ID: {empresaInDb.id_empresa})"
                );
                
                empresa.nombreempresa = empresa.nombreempresa?.ToUpper();
                
                _context.Entry(empresaInDb).CurrentValues.SetValues(empresa);
                _context.SaveChanges();
                
                _historialService.GuardarHistorial(
                    "DESPUES_EDITAR", 
                    empresa, 
                    "Empresas", 
                    $"Estado nuevo de empresa {empresa.nombreempresa} (ID: {empresa.id_empresa})"
                );
                
                return new JsonResult(Ok(empresa));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar empresa: {Id}", empresa.id_empresa);
                return new JsonResult(StatusCode(500, new { message = "Error al editar empresa", error = ex.Message }));
            }
        }

        // Get
        [HttpGet]
        public JsonResult Get(int id)
        {
            var result = _context.Empresas.Find(id);
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
                _logger.LogInformation($"Iniciando eliminación de empresa ID: {id}");
                
                var empresa = await _context.Empresas.FindAsync(id);
                if (empresa == null)
                {
                    return NotFound(new { message = "Empresa no encontrada" });
                }
                
                // Verificar si hay movimientos relacionados
                var tieneMovimientos = await _context.Movimientos
                    .AnyAsync(m => m.idempresa == id);
                    
                if (tieneMovimientos)
                {
                    _logger.LogWarning($"No se puede eliminar la empresa ID: {id} porque tiene movimientos asociados");
                    return BadRequest(new {
                        message = "No se puede eliminar esta empresa porque tiene movimientos asociados."
                    });
                }

                _historialService.GuardarHistorial("ELIMINAR", empresa, "Empresas", $"Eliminación de empresa {empresa.nombreempresa}");
                
                _context.Empresas.Remove(empresa);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Empresa ID: {id} eliminada exitosamente");
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar empresa: {Id}", id);
                return StatusCode(500, new { message = "Error al eliminar empresa", error = ex.Message });
            }
        }

        // Get All
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var result = await _context.Empresas.ToListAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener empresas");
                return StatusCode(500, new { message = "Error al obtener empresas", error = ex.Message });
            }
        }
    }
}
