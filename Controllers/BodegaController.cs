using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_de_Gestion_de_Importaciones.Models;
using Sistema_de_Gestion_de_Importaciones.Data;

namespace Sistema_de_Gestion_de_Importaciones.Controllers;

public class BodegaController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BodegaController> _logger;

    public BodegaController(ApplicationDbContext context, ILogger<BodegaController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Bodega
    public async Task<IActionResult> Index()
    {
        return View(await _context.Bodegas.ToListAsync());
    }

    // GET: Bodega/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var bodega = await _context.Bodegas
            .FirstOrDefaultAsync(m => m.IdBodega == id);
        if (bodega == null)
        {
            return NotFound();
        }

        return View(bodega);
    }

    // GET: Bodega/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Bodega/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("NombreBodega")] Bodega bodega)
    {
        if (ModelState.IsValid)
        {
            _context.Add(bodega);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(bodega);
    }

    // GET: Bodega/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var bodega = await _context.Bodegas.FindAsync(id);
        if (bodega == null)
        {
            return NotFound();
        }
        return View(bodega);
    }

    // POST: Bodega/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("IdBodega,NombreBodega")] Bodega bodega)
    {
        if (id != bodega.IdBodega)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(bodega);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BodegaExists(bodega.IdBodega))
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
        return View(bodega);
    }

    // GET: Bodega/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var bodega = await _context.Bodegas
            .FirstOrDefaultAsync(m => m.IdBodega == id);
        if (bodega == null)
        {
            return NotFound();
        }

        return View(bodega);
    }

    // POST: Bodega/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var bodega = await _context.Bodegas.FindAsync(id);
        if (bodega != null)
        {
            _context.Bodegas.Remove(bodega);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool BodegaExists(int id)
    {
        return _context.Bodegas.Any(e => e.IdBodega == id);
    }
}