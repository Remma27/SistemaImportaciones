using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sistema_de_Gestion_de_Importaciones.Models;
using Sistema_de_Gestion_de_Importaciones.Data;

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
        return View(await _context.Movimientos
            .Include(m => m.Importacion)
            .Include(m => m.Empresa)
            .ToListAsync());
    }

    // GET: Movimiento/Details/5
    public async Task<IActionResult> Details(int? id)
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
}