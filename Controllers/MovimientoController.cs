using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sistema_de_Gestion_de_Importaciones.Models;
using Sistema_de_Gestion_de_Importaciones.Data;
using Sistema_de_Gestion_de_Importaciones.Models.ViewModels;
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

    [HttpGet]
    [Route("Operaciones/RegistroImportacion")]
    public async Task<IActionResult> RegistroImportacion(int? importacionId = null)
    {
        try
        {
            _logger.LogInformation("Starting RegistroImportacion with importacionId: {0}", importacionId);

            // Get importaciones for dropdown
            var importaciones = await _context.Importaciones
                .Include(i => i.Barco)
                .OrderByDescending(i => i.Id)
                .ToListAsync();

            _logger.LogInformation("Found {0} importaciones", importaciones.Count);

            var selectListItems = importaciones.Select(i => new SelectListItem
            {
                Value = i.Id.ToString(),
                Text = $"#{i.Id} - {i.Barco?.NombreBarco ?? "Sin Barco"} - ({i.FechaHoraSystema:dd/MM/yyyy})",
                Selected = i.Id == importacionId
            }).ToList();

            selectListItems.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "Seleccione una importación...",
                Selected = !importacionId.HasValue
            });

            ViewBag.Importaciones = selectListItems;

            // Solo buscar movimientos si hay una importación seleccionada
            var movimientos = new List<RegistroImportacionViewModel>();

            if (importacionId.HasValue)
            {
                _logger.LogInformation("Fetching movements for importacion {0}", importacionId);

                movimientos = await _context.Movimientos
                    .Include(m => m.Importacion!)
                        .ThenInclude(i => i!.Barco)
                    .Include(m => m.Empresa)
                    .Where(m => m.IdImportacion == importacionId)
                    .OrderByDescending(m => m.FechaHora)
                    .Select(m => new RegistroImportacionViewModel
                    {
                        Id = m.Id,
                        Fecha = m.FechaHora ?? DateTime.Now,
                        NombreBarco = m.Importacion == null || m.Importacion.Barco == null ? "Sin Barco" : m.Importacion.Barco.NombreBarco ?? "Sin Barco",
                        NombreEmpresa = m.Empresa == null ? "Sin Empresa" : m.Empresa.NombreEmpresa ?? "Sin Empresa",
                        CantidadRequerida = (decimal)(m.CantidadRequerida ?? 0),
                        CantidadCamiones = m.CantidadCamiones ?? 0
                    })
                    .ToListAsync();

                _logger.LogInformation("Found {0} movements", movimientos.Count);
            }

            return View("~/Views/Operaciones/RegistroImportacion.cshtml", movimientos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RegistroImportacion");
            return View("~/Views/Operaciones/RegistroImportacion.cshtml",
                new List<RegistroImportacionViewModel>());
        }
    }

    public class RegistroImportacionViewModel
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public required string NombreBarco { get; set; }
        public required string NombreEmpresa { get; set; }
        public decimal CantidadRequerida { get; set; }
        public int CantidadCamiones { get; set; }
    }
}