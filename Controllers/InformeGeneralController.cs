using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using Sistema_de_Gestion_de_Importaciones.ViewModels;

namespace Sistema_de_Gestion_de_Importaciones.Controllers
{
    [Authorize(Roles = "Administrador,Operador, Reporteria")]
    public class InformeGeneralController : Controller
    {
        private readonly IMovimientoService _movimientoService;
        private readonly IBarcoService _barcoService;

        public InformeGeneralController(IMovimientoService movimientoService, IBarcoService barcoService)
        {
            _movimientoService = movimientoService;
            _barcoService = barcoService;
        }

        public async Task<IActionResult> Index(int? selectedBarco)
        {
            ViewData["FullWidth"] = true;
            var viewModel = new RegistroPesajesViewModel();

            var barcos = await _movimientoService.GetBarcosSelectListAsync();
            ViewBag.Barcos = new SelectList(barcos, "Value", "Text", selectedBarco);

            if (selectedBarco.HasValue)
            {
                try
                {
                    var barcoItem = barcos.FirstOrDefault(b => b.Value == selectedBarco.Value.ToString());
                    if (barcoItem != null)
                    {
                        var fullText = barcoItem.Text;
                        var parts = fullText.Split('-');
                        if (parts.Length >= 3)
                        {
                            viewModel.NombreBarco = parts[2].Trim();
                        }
                        else
                        {
                            viewModel.NombreBarco = fullText;
                        }
                    }
                    else
                    {
                        viewModel.NombreBarco = "Sin nombre";
                    }

                    ViewData["NombreBarco"] = viewModel.NombreBarco;

                    var informeGeneralData = await _movimientoService.GetInformeGeneralAsync(selectedBarco.Value);
                    var escotillasData = await _movimientoService.GetEscotillasDataAsync(selectedBarco.Value);

                    ViewData["NombreBarco"] = viewModel.NombreBarco;

                    viewModel.Tabla2Data = informeGeneralData.Select(ig => new RegistroPesajesAgregado
                    {
                        Agroindustria = ig.Empresa ?? "Sin nombre",
                        KilosRequeridos = (decimal)ig.RequeridoKg,
                        ToneladasRequeridas = (decimal)ig.RequeridoTon,
                        DescargaKilos = (decimal)ig.DescargaKg,
                        FaltanteKilos = (decimal)ig.FaltanteKg,
                        ToneladasFaltantes = (decimal)ig.TonFaltantes,
                        CamionesFaltantes = (decimal)ig.CamionesFaltantes,
                        ConteoPlacas = ig.ConteoPlacas,
                        PorcentajeDescarga = (decimal)ig.PorcentajeDescarga
                    }).ToList();

                    if (escotillasData != null)
                    {
                        viewModel.EscotillasData = escotillasData.Escotillas;
                        viewModel.CapacidadTotal = escotillasData.CapacidadTotal;
                        viewModel.DescargaTotal = escotillasData.DescargaTotal;
                        viewModel.DiferenciaTotal = escotillasData.DiferenciaTotal;
                        viewModel.PorcentajeTotal = escotillasData.PorcentajeTotal;
                        viewModel.EstadoGeneral = escotillasData.EstadoGeneral;
                        viewModel.TotalKilosRequeridos = escotillasData.TotalKilosRequeridos;

                        ViewData["KilosRequeridos"] = escotillasData.TotalKilosRequeridos;
                        ViewData["EstadoGeneral"] = escotillasData.EstadoGeneral;

                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error al cargar el informe: {ex.Message}");
                }
            }

            return View(viewModel);
        }
    }
}