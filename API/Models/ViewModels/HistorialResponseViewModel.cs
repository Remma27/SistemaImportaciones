using System;
using System.Collections.Generic;

namespace Sistema_de_Gestion_de_Importaciones.ViewModels
{
    public class HistorialResponseViewModel
    {
        public int TotalRegistros { get; set; }
        public List<HistorialViewModel> Registros { get; set; } = new List<HistorialViewModel>();
    }
}
