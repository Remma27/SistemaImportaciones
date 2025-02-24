using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sistema_de_Gestion_de_Importaciones.ViewModels;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;

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
            // Cargar dropdowns
            var barcosList = await _movimientoService.GetBarcosSelectListAsync();
            ViewBag.Barcos = new SelectList(barcosList, "Value", "Text", selectedBarco);
            ViewBag.Empresas = new SelectList(await _movimientoService.GetEmpresasSelectListAsync(), "Value", "Text", empresaId);

            var viewModel = new RegistroPesajesViewModel
            {
                Tabla1Data = new List<RegistroPesajesIndividual>(),
                Tabla2Data = new List<RegistroPesajesAgregado>(),
                TotalesPorBodega = new List<TotalesPorBodegaViewModel>(),
                EscotillasData = new List<EscotillaViewModel>()
            };

            if (selectedBarco.HasValue)
            {
                // Primero obtener el informe general para tener los totales
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

                // Luego obtener información del barco
                var importacion = await _importacionService.GetByIdAsync(selectedBarco.Value);
                if (importacion?.Barco != null)
                {
                    var barco = importacion.Barco;
                    viewModel.Barco = barco;

                    // Obtener totales (descargado se suma de las empresas)
                    decimal totalDescargado = viewModel.Tabla2Data.Sum(x => x.DescargaKilos);
                    decimal totalRequerido = viewModel.Tabla2Data.Sum(x => x.KilosRequeridos);

                    // Calcular capacidad total del barco (es decir, la suma de las escotillas)
                    decimal capacidadTotal = 0;
                    var escotillasActivas = new Dictionary<int, decimal>();

                    // Iterar hasta escotilla7 (según se tenga configurado)
                    for (int i = 1; i <= 7; i++)
                    {
                        var capacidad = (decimal?)barco.GetType().GetProperty($"escotilla{i}")?.GetValue(barco) ?? 0;
                        if (capacidad > 0)
                        {
                            escotillasActivas.Add(i, capacidad);
                            capacidadTotal += capacidad;
                        }
                    }

                    // Distribuir la descarga total de forma proporcional al valor de cada escotilla
                    viewModel.EscotillasData = new List<EscotillaViewModel>();
                    foreach (var escotilla in escotillasActivas)
                    {
                        // El valor "requerido" para la escotilla es su capacidad
                        decimal requeridaEscotilla = escotilla.Value;

                        // Proporción que representa la escotilla en el total
                        decimal proporcion = capacidadTotal > 0 ? escotilla.Value / capacidadTotal : 0;

                        // Se distribuye la descarga total según la misma proporción
                        decimal descargaReal = Math.Round(totalDescargado * proporcion, 0);

                        // Diferencia: faltante (positivo) o excedente (negativo)
                        decimal diferencia = requeridaEscotilla - descargaReal;

                        // Porcentaje de descarga para la escotilla (evitar división por cero)
                        decimal porcentaje = requeridaEscotilla > 0
                            ? Math.Round((descargaReal * 100.0M / requeridaEscotilla), 2)
                            : 0;

                        viewModel.EscotillasData.Add(new EscotillaViewModel
                        {
                            Numero = escotilla.Key,
                            Capacidad = escotilla.Value,
                            DescargaEsperada = requeridaEscotilla,
                            DescargaReal = descargaReal,
                            Diferencia = diferencia,
                            Porcentaje = porcentaje
                        });
                    }

                    // Actualizar totales del barco
                    viewModel.TotalKilosRequeridos = capacidadTotal;
                    viewModel.TotalDescargaKilos = totalDescargado;
                    viewModel.TotalKilosFaltantes = capacidadTotal - totalDescargado;
                    viewModel.PorcentajeTotal = capacidadTotal > 0
                        ? Math.Round((totalDescargado * 100.0M / capacidadTotal), 2)
                        : 0;
                }

                // Procesar datos de empresa si está seleccionada
                if (empresaId.HasValue)
                {
                    // Obtener y calcular datos por empresa
                    var calculo = await _movimientoService.CalculoMovimientos(importacionId, empresaId.Value);
                    if (calculo != null)
                    {
                        // Mapear nombres de bodegas
                        var bodegas = await _bodegaService.GetAllAsync();
                        var bodegasDict = bodegas.ToDictionary(b => b.id, b => b.bodega ?? "Sin nombre");

                        // Generar datos para Tabla1
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