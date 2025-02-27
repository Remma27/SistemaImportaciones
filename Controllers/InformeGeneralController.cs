using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using Sistema_de_Gestion_de_Importaciones.Models.ViewModels;

namespace Sistema_de_Gestion_de_Importaciones.Controllers
{
    public class InformeGeneralController : Controller
    {
        private readonly IMovimientoService _movimientoService;
        private readonly ILogger<InformeGeneralController> _logger;

        public InformeGeneralController(IMovimientoService registroService,
                                                ILogger<InformeGeneralController> logger)
        {
            _movimientoService = registroService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int? selectedBarco)
        {
            try
            {
                var barcos = await _movimientoService.GetBarcosSelectListAsync();
                ViewBag.Barcos = new SelectList(barcos, "Value", "Text", selectedBarco);

                if (selectedBarco.HasValue)
                {
                    // Get the informe general data
                    var informeGeneral = await _movimientoService.GetInformeGeneralAsync(selectedBarco.Value);

                    // Get the total count from the property set in the service
                    ViewBag.TotalMovimientos = _movimientoService.TotalMovimientos;

                    // Get escotillas data
                    try
                    {
                        var escotillasData = await _movimientoService.GetEscotillasDataAsync(selectedBarco.Value);
                        ViewBag.EscotillasData = escotillasData;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting escotillas data: {Message}", ex.Message);
                        // Continue without escotillas data
                    }

                    return View(informeGeneral);
                }

                return View(new List<InformeGeneralViewModel>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading informe general: {Message}", ex.Message);
                TempData["Error"] = "Error al cargar el informe: " + ex.Message;
                return View(new List<InformeGeneralViewModel>());
            }
        }
    }
}