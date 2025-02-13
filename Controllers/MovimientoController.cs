using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sistema_de_Gestion_de_Importaciones.Models;
using Sistema_de_Gestion_de_Importaciones.Data;
using Sistema_de_Gestion_de_Importaciones.Models.ViewModels;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using Sistema_de_Gestion_de_Importaciones.Helpers;

namespace Sistema_de_Gestion_de_Importaciones.Controllers;

public class MovimientoController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MovimientoController> _logger;

    public MovimientoController(ApplicationDbContext context, ILogger<MovimientoController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Movimiento
    public async Task<IActionResult> Index()
    {
        try
        {
            var query = "SELECT * FROM movimientos";
            var movimientos = await _context.Movimientos
                .FromSqlRaw(query)
                .Include(m => m.Importacion)
                .Include(m => m.Empresa)
                .AsNoTracking()
                .ToListAsync();

            return View(movimientos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading movimientos");
            // Return empty list instead of failing
            return View(new List<Movimiento>());
        }
    }

    // GET: Movimiento/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        try
        {
            var query = "SELECT * FROM movimientos WHERE id = {0}";
            var movimiento = await _context.Movimientos
                .FromSqlRaw(query, id)
                .Include(m => m.Importacion)
                .Include(m => m.Empresa)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (movimiento == null)
            {
                return NotFound();
            }

            return View(movimiento);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading movimiento details for ID: {Id}", id);
            return NotFound();
        }
    }

    // GET: Movimiento/Create
    public IActionResult Create()
    {
        ViewData["IdImportacion"] = new SelectList(_context.Importaciones, "Id", "Id");
        ViewData["IdEmpresa"] = new SelectList(_context.Empresas, "IdEmpresa", "NombreEmpresa");
        ViewData["Bodegas"] = new SelectList(_context.Bodegas, "IdBodega", "NombreBodega");
        return View();
    }

    // POST: Movimiento/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("IdImportacion,IdEmpresa,TipoTransaccion,CantidadCamiones,CantidadRequerida,CantidadEntregada,Placa,PlacaAlterna,Guia,GuiaAlterna,Escotilla,TotalCarga,Bodega")] Movimiento movimiento)
    {
        if (ModelState.IsValid)
        {
            movimiento.IdUsuario = HttpContext.User.GetUserId();
            movimiento.FechaHoraSystema = DateTime.Now;
            movimiento.FechaHora = DateTime.Now;
            _context.Add(movimiento);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        ViewData["IdImportacion"] = new SelectList(_context.Importaciones, "Id", "Id", movimiento.IdImportacion);
        ViewData["IdEmpresa"] = new SelectList(_context.Empresas, "IdEmpresa", "NombreEmpresa", movimiento.IdEmpresa);
        ViewData["Bodegas"] = new SelectList(_context.Bodegas, "IdBodega", "NombreBodega", movimiento.Bodega);
        return View(movimiento);
    }

    // GET: Movimiento/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var movimiento = await _context.Movimientos.FindAsync(id);
        if (movimiento == null)
        {
            return NotFound();
        }

        ViewData["IdImportacion"] = new SelectList(_context.Importaciones, "Id", "Id", movimiento.IdImportacion);
        ViewData["IdEmpresa"] = new SelectList(_context.Empresas, "IdEmpresa", "NombreEmpresa", movimiento.IdEmpresa);
        ViewData["Bodegas"] = new SelectList(_context.Bodegas, "IdBodega", "NombreBodega", movimiento.Bodega);
        return View(movimiento);
    }

    // POST: Movimiento/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,IdImportacion,IdEmpresa,TipoTransaccion,CantidadCamiones,CantidadRequerida,CantidadEntregada,Placa,PlacaAlterna,Guia,GuiaAlterna,Escotilla,TotalCarga,Bodega,FechaHora,FechaHoraSystema")] Movimiento movimiento)
    {
        if (id != movimiento.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                movimiento.IdUsuario = HttpContext.User.GetUserId();
                _context.Update(movimiento);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MovimientoExists(movimiento.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }

        ViewData["IdImportacion"] = new SelectList(_context.Importaciones, "Id", "Id", movimiento.IdImportacion);
        ViewData["IdEmpresa"] = new SelectList(_context.Empresas, "IdEmpresa", "NombreEmpresa", movimiento.IdEmpresa);
        ViewData["Bodegas"] = new SelectList(_context.Bodegas, "IdBodega", "NombreBodega", movimiento.Bodega);
        return View(movimiento);
    }

    // GET: Movimiento/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var movimiento = await _context.Movimientos
            .Include(m => m.Importacion)
            .Include(m => m.Empresa)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (movimiento == null)
        {
            return NotFound();
        }

        return View(movimiento);
    }

    // POST: Movimiento/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var movimiento = await _context.Movimientos.FindAsync(id);
        if (movimiento != null)
        {
            _context.Movimientos.Remove(movimiento);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool MovimientoExists(int id)
    {
        return _context.Movimientos.Any(e => e.Id == id);
    }

    // GET: Movimiento/InformeGeneral
    [Route("Operaciones/InformeGeneral")]
    [HttpGet]
    public async Task<IActionResult> InformeGeneral(int? importacionId = null)
    {
        try
        {
            _logger.LogInformation("Starting InformeGeneral data retrieval");

            // Debug: Check importaciones data
            var importacionesCount = await _context.Importaciones.CountAsync();
            _logger.LogInformation($"Total importaciones in database: {importacionesCount}");

            // Get importaciones for dropdown with more details
            var importaciones = await _context.Importaciones
                .Include(i => i.Barco)
                .OrderByDescending(i => i.Id)
                .Select(i => new
                {
                    i.Id,
                    BarcoNombre = i.Barco != null ? i.Barco.NombreBarco : "Sin Barco",
                    FechaRegistro = i.FechaHoraSystema
                })
                .ToListAsync();

            _logger.LogInformation($"Retrieved {importaciones.Count} importaciones");

            // Create dropdown items with more descriptive text
            var selectListItems = importaciones.Select(i => new SelectListItem
            {
                Value = i.Id.ToString(),
                Text = $"#{i.Id} - {i.BarcoNombre} - ({i.FechaRegistro:dd/MM/yyyy})"
            }).ToList();

            // Add "All" option at the beginning
            selectListItems.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "Todas las Importaciones",
                Selected = !importacionId.HasValue
            });

            ViewBag.Importaciones = selectListItems;

            // Log selected importacion
            if (importacionId.HasValue)
            {
                var selectedImportacion = importaciones.FirstOrDefault(i => i.Id == importacionId);
                _logger.LogInformation($"Selected importacion: {(selectedImportacion?.Id.ToString() ?? "None")}");
            }

            // Obtener datos y analizar la estructura
            var analysis = await _context.Movimientos
                .Include(m => m.Empresa)
                .Where(m => !importacionId.HasValue || m.IdImportacion == importacionId)
                .Select(m => new
                {
                    m.IdEmpresa,
                    EmpresaNombre = m.Empresa != null ? m.Empresa.NombreEmpresa : "Sin Empresa",
                    m.IdImportacion,
                    m.CantidadRequerida,
                    m.CantidadEntregada,
                })
                .ToListAsync();

            // Primero agrupar por empresa
            var groupedByEmpresa = analysis
                .GroupBy(m => new { m.IdEmpresa, m.EmpresaNombre })
                .Select(g => new
                {
                    g.Key.IdEmpresa,
                    g.Key.EmpresaNombre,
                    ImportacionesCount = g.Select(x => x.IdImportacion).Distinct().Count(),
                    MovimientosCount = g.Count(),
                    TotalRequerido = g.Sum(x => x.CantidadRequerida ?? 0),
                    TotalEntregado = g.Sum(x => x.CantidadEntregada ?? 0)
                })
                .ToList();

            // Log de análisis
            foreach (var g in groupedByEmpresa.Take(5))
            {
                _logger.LogInformation($"Empresa: {g.EmpresaNombre}, " +
                    $"Importaciones: {g.ImportacionesCount}, " +
                    $"Movimientos: {g.MovimientosCount}, " +
                    $"Total Req: {g.TotalRequerido:N2}, " +
                    $"Total Ent: {g.TotalEntregado:N2}");
            }

            // Crear el informe final
            var informeData = groupedByEmpresa
                .Select(g => new InformeGeneralViewModel
                {
                    Empresa = g.EmpresaNombre ?? "Sin Empresa",
                    RequeridoKg = g.TotalRequerido,
                    DescargaKg = g.TotalEntregado,
                    RequeridoTon = g.TotalRequerido / 1000,
                    FaltanteKg = g.TotalRequerido - g.TotalEntregado,
                    TonFaltantes = (g.TotalRequerido - g.TotalEntregado) / 1000,
                    CamionesFaltantes = (int)Math.Ceiling((g.TotalRequerido - g.TotalEntregado) / 30000),
                    ConteoPlacas = _context.Movimientos
                        .Where(m => m.IdEmpresa == g.IdEmpresa && m.Placa != null)
                        .Select(m => m.Placa)
                        .Distinct()
                        .Count(),
                    PorcentajeDescarga = g.TotalRequerido > 0
                        ? (g.TotalEntregado / g.TotalRequerido) * 100
                        : 0
                })
                .OrderBy(x => x.Empresa)
                .ToList();

            ViewBag.DataCount = informeData.Count;
            ViewBag.TotalRegistros = analysis.Count;
            ViewBag.HasData = informeData.Any();
            ViewBag.ImportacionesCount = analysis.Select(x => x.IdImportacion).Distinct().Count();
            ViewBag.SelectedImportacion = importacionId;

            return View("~/Views/Operaciones/InformeGeneral.cshtml", informeData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in InformeGeneral");
            throw;
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetInformeByImportacion(int importacionId)
    {
        try
        {
            return await InformeGeneral(importacionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting informe for importación: {ImportacionId}", importacionId);
            return Json(new { error = "Error loading data" });
        }
    }

    [HttpGet]
    [Route("Movimiento/ExportToExcel")]
    public async Task<IActionResult> ExportToExcel(int? importacionId = null)
    {
        try
        {
            var informeData = await GetInformeData(importacionId);

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Informe General");

                // Encabezados
                worksheet.Cells[1, 1].Value = "Empresa";
                worksheet.Cells[1, 2].Value = "Req. KG";
                worksheet.Cells[1, 3].Value = "Req. Toneladas";
                worksheet.Cells[1, 4].Value = "Descarga KG";
                worksheet.Cells[1, 5].Value = "Faltante KG";
                worksheet.Cells[1, 6].Value = "Ton. Faltantes";
                worksheet.Cells[1, 7].Value = "Camiones Faltantes";
                worksheet.Cells[1, 8].Value = "Conteo Placas";
                worksheet.Cells[1, 9].Value = "% Descarga";

                // Estilo de encabezados
                using (var range = worksheet.Cells[1, 1, 1, 9])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                // Datos
                for (int i = 0; i < informeData.Count; i++)
                {
                    var item = informeData[i];
                    worksheet.Cells[i + 2, 1].Value = item.Empresa;
                    worksheet.Cells[i + 2, 2].Value = item.RequeridoKg;
                    worksheet.Cells[i + 2, 3].Value = item.RequeridoTon;
                    worksheet.Cells[i + 2, 4].Value = item.DescargaKg;
                    worksheet.Cells[i + 2, 5].Value = item.FaltanteKg;
                    worksheet.Cells[i + 2, 6].Value = item.TonFaltantes;
                    worksheet.Cells[i + 2, 7].Value = item.CamionesFaltantes;
                    worksheet.Cells[i + 2, 8].Value = item.ConteoPlacas;
                    worksheet.Cells[i + 2, 9].Value = item.PorcentajeDescarga;
                }

                // Ajustar ancho de columnas
                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var fileName = $"InformeGeneral_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting to Excel");
            return RedirectToAction(nameof(InformeGeneral), new { importacionId });
        }
    }

    private async Task<List<InformeGeneralViewModel>> GetInformeData(int? importacionId)
    {
        // Obtener datos y analizar la estructura
        var analysis = await _context.Movimientos
            .Include(m => m.Empresa)
            .Where(m => !importacionId.HasValue || m.IdImportacion == importacionId)
            .Select(m => new
            {
                m.IdEmpresa,
                EmpresaNombre = m.Empresa != null ? m.Empresa.NombreEmpresa : "Sin Empresa",
                m.IdImportacion,
                m.CantidadRequerida,
                m.CantidadEntregada,
            })
            .ToListAsync();

        // Primero agrupar por empresa
        var groupedByEmpresa = analysis
            .GroupBy(m => new { m.IdEmpresa, m.EmpresaNombre })
            .Select(g => new
            {
                g.Key.IdEmpresa,
                g.Key.EmpresaNombre,
                ImportacionesCount = g.Select(x => x.IdImportacion).Distinct().Count(),
                MovimientosCount = g.Count(),
                TotalRequerido = g.Sum(x => x.CantidadRequerida ?? 0),
                TotalEntregado = g.Sum(x => x.CantidadEntregada ?? 0)
            })
            .ToList();

        // Crear el informe final
        var informeData = groupedByEmpresa
            .Select(g => new InformeGeneralViewModel
            {
                Empresa = g.EmpresaNombre ?? "Sin Empresa",
                RequeridoKg = g.TotalRequerido,
                DescargaKg = g.TotalEntregado,
                RequeridoTon = g.TotalRequerido / 1000,
                FaltanteKg = g.TotalRequerido - g.TotalEntregado,
                TonFaltantes = (g.TotalRequerido - g.TotalEntregado) / 1000,
                CamionesFaltantes = (int)Math.Ceiling((g.TotalRequerido - g.TotalEntregado) / 30000),
                ConteoPlacas = _context.Movimientos
                    .Where(m => m.IdEmpresa == g.IdEmpresa && m.Placa != null)
                    .Select(m => m.Placa)
                    .Distinct()
                    .Count(),
                PorcentajeDescarga = g.TotalRequerido > 0
                    ? (g.TotalEntregado / g.TotalRequerido) * 100
                    : 0
            })
            .OrderBy(x => x.Empresa)
            .ToList();

        return informeData;
    }

    private List<InformeGeneralViewModel> GetTestData()
    {
        _logger.LogInformation("Generating test data");
        return new List<InformeGeneralViewModel>
        {
            new InformeGeneralViewModel
            {
                Empresa = "Empresa Test 1",
                RequeridoKg = 50000,
                RequeridoTon = 50,
                DescargaKg = 30000,
                FaltanteKg = 20000,
                TonFaltantes = 20,
                CamionesFaltantes = 1,
                ConteoPlacas = 2,
                PorcentajeDescarga = 60
            },
            new InformeGeneralViewModel
            {
                Empresa = "Empresa Test 2",
                RequeridoKg = 75000,
                RequeridoTon = 75,
                DescargaKg = 25000,
                FaltanteKg = 50000,
                TonFaltantes = 50,
                CamionesFaltantes = 2,
                ConteoPlacas = 1,
                PorcentajeDescarga = 33.33
            }
        };
    }
}