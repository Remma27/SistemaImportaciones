using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using Sistema_de_Gestion_de_Importaciones.ViewModels;

namespace Sistema_de_Gestion_de_Importaciones.Controllers
{
    [Authorize]
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
            ViewBag.Barcos = new SelectList(await _movimientoService.GetBarcosSelectListAsync(), "Value", "Text", selectedBarco);

            if (!selectedBarco.HasValue)
            {
                return View(viewModel);
            }

            try
            {
                // Get the report data
                var informeGeneralData = await _movimientoService.GetInformeGeneralAsync(selectedBarco.Value);
                var escotillasData = await _movimientoService.GetEscotillasDataAsync(selectedBarco.Value);

                // Map to ViewModel
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

                    // Establecer ViewData que será utilizado por la vista parcial
                    ViewData["KilosRequeridos"] = escotillasData.TotalKilosRequeridos;
                    ViewData["EstadoGeneral"] = escotillasData.EstadoGeneral;

                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // Log the exception
                // _logger.LogError(ex, "Error loading informe general");
                ModelState.AddModelError("", $"Error al cargar el informe: {ex.Message}");
                return View(viewModel);
            }
        }
    }
}