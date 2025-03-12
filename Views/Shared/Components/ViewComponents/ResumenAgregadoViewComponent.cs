using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using Sistema_de_Gestion_de_Importaciones.ViewModels;
using System.Collections.Generic;

namespace Sistema_de_Gestion_de_Importaciones.ViewComponents
{
    public class ResumenAgregadoViewComponent : ViewComponent
    {
        private readonly IMovimientoService _movimientoService;
        private readonly ILogger<ResumenAgregadoViewComponent> _logger;

        public ResumenAgregadoViewComponent(IMovimientoService movimientoService, ILogger<ResumenAgregadoViewComponent> logger)
        {
            _movimientoService = movimientoService;
            _logger = logger;
        }

        public IViewComponentResult Invoke(IEnumerable<RegistroPesajesAgregado> model)
        {
            // Get the totalMovimientos value from the service and pass it to the view
            ViewBag.TotalMovimientos = _movimientoService.TotalMovimientos;
            _logger.LogInformation($"ResumenAgregado ViewComponent - Setting TotalMovimientos to: {_movimientoService.TotalMovimientos}");

            return View(model);
        }
    }
}
