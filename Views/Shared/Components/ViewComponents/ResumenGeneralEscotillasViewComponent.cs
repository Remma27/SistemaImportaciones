using Microsoft.AspNetCore.Mvc;
using Sistema_de_Gestion_de_Importaciones.ViewModels;
using System.Collections.Generic;

namespace Sistema_de_Gestion_de_Importaciones.ViewComponents
{
    public class ResumenGeneralEscotillasViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(
            IEnumerable<EscotillaViewModel> model,
            decimal capacidadTotal,
            decimal descargaTotal,
            decimal diferenciaTotal,
            decimal porcentajeTotal,
            string estadoGeneral,
            decimal kilosRequeridos = 0)
        {
            ViewData["CapacidadTotal"] = capacidadTotal;
            ViewData["DescargaTotal"] = descargaTotal;
            ViewData["DiferenciaTotal"] = diferenciaTotal;
            ViewData["PorcentajeTotal"] = porcentajeTotal;
            ViewData["EstadoGeneral"] = estadoGeneral;
            ViewData["KilosRequeridos"] = kilosRequeridos;

            return View(model);
        }
    }
}
