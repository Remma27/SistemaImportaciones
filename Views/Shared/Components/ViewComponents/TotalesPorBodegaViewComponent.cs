using Microsoft.AspNetCore.Mvc;
using Sistema_de_Gestion_de_Importaciones.Models;
using System.Collections.Generic;

namespace Sistema_de_Gestion_de_Importaciones.ViewComponents
{
    public class TotalesPorBodegaViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(IEnumerable<dynamic> model)
        {
            return View(model);
        }
    }
}
