using Microsoft.AspNetCore.Mvc;
using Sistema_de_Gestion_de_Importaciones.ViewModels;
using System.Collections.Generic;

namespace Sistema_de_Gestion_de_Importaciones.ViewComponents
{
    public class ResumenAgregadoViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(IEnumerable<RegistroPesajesAgregado> model)
        {
            return View(model ?? new List<RegistroPesajesAgregado>());
        }
    }
}
