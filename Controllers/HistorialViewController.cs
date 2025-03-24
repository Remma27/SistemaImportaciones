using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sistema_de_Gestion_de_Importaciones.Models;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using System.Text.Json;

namespace Sistema_de_Gestion_de_Importaciones.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class HistorialViewController : Controller
    {
        private readonly IHistorialService _historialService;
        private readonly ILogger<HistorialViewController> _logger;

        public HistorialViewController(IHistorialService historialService, ILogger<HistorialViewController> logger)
        {
            _historialService = historialService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var historial = await _historialService.ObtenerTodoAsync();
                _logger.LogInformation("Mostrando todos los {Count} registros históricos del sistema", historial.TotalRegistros);
                return View(historial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener historial");
                return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
            }
        }

        public async Task<IActionResult> Detalle(int id)
        {
            try
            {
                var registro = await _historialService.ObtenerPorIdAsync(id);
                if (registro == null || registro.Id == 0)
                {
                    return NotFound();
                }
                return View(registro);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener detalle del historial con ID: {Id}", id);
                return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
            }
        }
        
        public async Task<IActionResult> VerJSON(int id)
        {
            try
            {
                var registro = await _historialService.ObtenerPorIdAsync(id);
                if (registro == null || registro.Id == 0)
                {
                    return NotFound();
                }
                
                try
                {
                    var jsonObject = JsonSerializer.Deserialize<JsonElement>(registro.DatosJSON);
                    var formattedJson = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });
                    ViewBag.FormattedJSON = formattedJson;
                }
                catch
                {
                    ViewBag.FormattedJSON = registro.DatosJSON;
                }
                
                return View(registro);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al visualizar JSON del historial con ID: {Id}", id);
                return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
            }
        }
    }
}
