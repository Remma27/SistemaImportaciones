using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sistema_de_Gestion_de_Importaciones.ViewModels;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using Sistema_de_Gestion_de_Importaciones.Services;

namespace Sistema_de_Gestion_de_Importaciones.Controllers
{
    public class RegistroPesajesController : Controller
    {
        private readonly IMovimientoService _movimientoService;
        private readonly IBodegaService _bodegaService;
        private readonly ILogger<RegistroPesajesController> _logger;
        private readonly IImportacionService _importacionService;

        public RegistroPesajesController(
            IMovimientoService movimientoService,
            IBodegaService bodegaService,
            ILogger<RegistroPesajesController> logger,
            IImportacionService importacionService)
        {
            _movimientoService = movimientoService;
            _bodegaService = bodegaService;
            _logger = logger;
            _importacionService = importacionService;
        }

        // GET: RegistroPesajes/Index
        public async Task<IActionResult> Index(int? selectedBarco, int? empresaId)
        {
            // Cargar dropdown de barcos
            var barcosList = await _movimientoService.GetBarcosSelectListAsync();
            ViewBag.Barcos = new SelectList(barcosList, "Value", "Text", selectedBarco);
            ViewBag.Empresas = new SelectList(await _movimientoService.GetEmpresasSelectListAsync(), "Value", "Text", empresaId);

            var viewModel = new RegistroPesajesViewModel
            {
                Tabla1Data = new List<RegistroPesajesIndividual>(),
                Tabla2Data = new List<RegistroPesajesAgregado>(),
                TotalesPorBodega = new List<TotalesPorBodegaViewModel>()
            };

            if (selectedBarco.HasValue)
            {
                int importacionId = selectedBarco.Value;
                var informeGeneral = await _movimientoService.GetInformeGeneralAsync(importacionId);
                viewModel.Tabla2Data = informeGeneral.Select(ig => new RegistroPesajesAgregado
                {
                    Agroindustria = ig.Empresa,
                    KilosRequeridos = (decimal)ig.RequeridoKg,
                    ToneladasRequeridas = (decimal)ig.RequeridoTon,
                    DescargaKilos = (decimal)ig.DescargaKg,
                    FaltanteKilos = (decimal)ig.FaltanteKg,
                    ToneladasFaltantes = (decimal)ig.TonFaltantes,
                    CamionesFaltantes = (decimal)ig.CamionesFaltantes,
                    ConteoPlacas = ig.ConteoPlacas,
                    PorcentajeDescarga = (decimal)ig.PorcentajeDescarga
                }).ToList();

                if (empresaId.HasValue)
                {
                    var calculo = await _movimientoService.CalculoMovimientos(importacionId, empresaId.Value);
                    if (calculo != null)
                    {
                        // Obtener lista de bodegas para mapear IDs a nombres
                        var bodegas = await _bodegaService.GetAllAsync();
                        var bodegasDict = bodegas.ToDictionary(b => b.id, b => b.bodega ?? "Sin nombre");

                        viewModel.Tabla1Data = calculo.Select(c => new RegistroPesajesIndividual
                        {
                            Id = c.id,
                            Bodega = c.bodega.HasValue && bodegasDict.ContainsKey(c.bodega.Value) ? bodegasDict[c.bodega.Value] : "Sin Bodega",
                            Guia = c.guia?.ToString() ?? "Sin Guía",
                            Placa = c.placa ?? "Sin Placa",
                            PesoRequerido = c.cantidadrequerida.GetValueOrDefault(),
                            PesoEntregado = c.cantidadentregada.GetValueOrDefault(),
                            PesoFaltante = c.peso_faltante,
                            Porcentaje = c.porcentaje
                        }).ToList();

                        // Calcular totales por bodega
                        viewModel.TotalesPorBodega = calculo
                            .GroupBy(c => c.bodega)
                            .Select(g => new TotalesPorBodegaViewModel
                            {
                                Bodega = g.Key.HasValue && bodegasDict.ContainsKey(g.Key.Value) ? bodegasDict[g.Key.Value] : "Sin Bodega",
                                TotalKilos = g.Sum(m => m.cantidadentregada ?? 0),
                                CantidadMovimientos = g.Count()
                            })
                            .ToList();
                    }
                }
            }

            return View(viewModel);
        }
    }
}