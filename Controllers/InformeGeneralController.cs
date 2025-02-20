using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sistema_de_Gestion_de_Importaciones.ViewModels;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using Sistema_de_Gestion_de_Importaciones.Helpers; // Para GetUserId()
using API.Models;
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

        // GET: InformeGeneral
        public async Task<IActionResult> Index(int? selectedBarco)
        {
            try
            {
                var barcos = await _movimientoService.GetBarcosSelectListAsync();
                ViewBag.Barcos = new SelectList(barcos, "Value", "Text", selectedBarco);

                // Only get data if a barco is selected
                var informeGeneral = selectedBarco.HasValue
                    ? await _movimientoService.GetInformeGeneralAsync(selectedBarco.Value)
                    : new List<InformeGeneralViewModel>();

                return View(informeGeneral);
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