using Microsoft.AspNetCore.Mvc;
using Sistema_de_Gestion_de_Importaciones.ViewModels;
using System.Collections.Generic;

namespace Sistema_de_Gestion_de_Importaciones.ViewComponents
{
    public class RegistrosIndividualesViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(List<RegistroPesajesIndividual> registros, object selectedBarco, object empresaId)
        {
            // Parse the values safely
            var selectedBarcoValue = 0;
            var empresaIdValue = 0;

            if (selectedBarco != null)
            {
                int.TryParse(selectedBarco.ToString(), out selectedBarcoValue);
            }

            if (empresaId != null)
            {
                int.TryParse(empresaId.ToString(), out empresaIdValue);
            }

            ViewBag.SelectedBarco = selectedBarcoValue;
            ViewBag.EmpresaId = empresaIdValue;

            return View(registros);
        }
    }
}
