using Microsoft.AspNetCore.Mvc;
using API.Data;
using Microsoft.AspNetCore.Authorization;
using API.Services;


namespace API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class SincronizacionController : ControllerBase
    {
        private readonly ApiContext _context;
        private readonly ILogger<SincronizacionController> _logger;
        private readonly ISyncService _syncService;

        public SincronizacionController(
            ApiContext context, 
            ILogger<SincronizacionController> logger, 
            ISyncService syncService)
        {
            _context = context;
            _logger = logger;
            _syncService = syncService;
        }

        [HttpGet]
        public async Task<IActionResult> ExportarDatos()
        {
            try
            {
                _logger.LogInformation("Iniciando exportación completa de todos los datos");
                
                var compressedStream = await _syncService.ExportarDatosAsync(DateTime.MinValue);
                
                compressedStream.Position = 0;
                
                var nombreArchivo = $"sync_datos_completo_{DateTime.Now:yyyyMMddHHmmss}.json.gz";
                
                return File(compressedStream, "application/gzip", nombreArchivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar datos para sincronización");
                return StatusCode(500, new { error = "Error al exportar datos", mensaje = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ImportarDatos(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
            {
                return BadRequest("No se proporcionó un archivo válido");
            }

            try
            {
                // Usar el servicio de sincronización para importar los datos
                var resultado = await _syncService.ImportarDatosAsync(archivo.OpenReadStream());
                
                if (resultado.Exitoso)
                {
                    return Ok(resultado);
                }
                else
                {
                    return BadRequest(resultado);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al importar archivo de sincronización");
                return StatusCode(500, new { 
                    Exitoso = false, 
                    Mensaje = $"Error: {ex.Message}" 
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EnviarPorCorreo([FromBody] SolicitudEnvioCorreo solicitud)
        {
            if (string.IsNullOrEmpty(solicitud?.Destinatario))
            {
                return BadRequest("Se requiere un correo electrónico de destino");
            }

            try
            {
                var resultado = await _syncService.EnviarPorCorreoAsync(solicitud.Destinatario, new MemoryStream());
                
                if (resultado)
                {
                    return Ok(new { 
                        Exitoso = true, 
                        Mensaje = $"Datos enviados exitosamente a {solicitud.Destinatario}" 
                    });
                }
                else
                {
                    return StatusCode(500, new { 
                        Exitoso = false, 
                        Mensaje = "Error al enviar correo electrónico" 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correo de sincronización");
                return StatusCode(500, new { 
                    Exitoso = false, 
                    Mensaje = $"Error: {ex.Message}" 
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerUltimaSincronizacion()
        {
            try
            {
                var ultimaFecha = await _syncService.ObtenerUltimaSincronizacionAsync();
                
                return Ok(new {
                    FechaUltimaSincronizacion = ultimaFecha
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la última fecha de sincronización");
                return StatusCode(500, new { 
                    Exitoso = false, 
                    Mensaje = $"Error: {ex.Message}" 
                });
            }
        }
    }

    public class SolicitudEnvioCorreo
    {
        public required string Destinatario { get; set; }
    }
}