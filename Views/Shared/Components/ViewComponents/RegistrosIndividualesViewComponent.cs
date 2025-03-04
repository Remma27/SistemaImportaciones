using Microsoft.AspNetCore.Mvc;
using Sistema_de_Gestion_de_Importaciones.ViewModels;
using System.Collections.Generic;

namespace Sistema_de_Gestion_de_Importaciones.ViewComponents
{
    public class RegistrosIndividualesViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(IEnumerable<RegistroPesajesIndividual> model)
        {
            var modelList = model?.ToList() ?? new List<RegistroPesajesIndividual>();
            ViewBag.SelectedBarco = HttpContext.Request.Query["selectedBarco"].ToString();
            ViewBag.EmpresaId = HttpContext.Request.Query["empresaId"].ToString();

            return View(modelList);
        }
    }
}
