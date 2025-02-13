using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sistema_de_Gestion_de_Importaciones.Models;
using Sistema_de_Gestion_de_Importaciones.Data;
using Sistema_de_Gestion_de_Importaciones.Helpers;

namespace Sistema_de_Gestion_de_Importaciones.Controllers;

public class ImportacionController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ImportacionController> _logger;

    public ImportacionController(ApplicationDbContext context, ILogger<ImportacionController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Importacion
    public async Task<IActionResult> Index()
    {
        return View(await _context.Importaciones
            .Include(i => i.Barco)
            .ToListAsync());
    }

    // GET: Importacion/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var importacion = await _context.Importaciones
            .Include(i => i.Barco)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (importacion == null)
        {
            return NotFound();
        }

        return View(importacion);
    }

    // GET: Importacion/Create
    public IActionResult Create()
    {
        ViewBag.Barcos = new SelectList(_context.Barcos, "Id", "NombreBarco");
        return View();
    }

    // POST: Importacion/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("FechaHora,IdBarco,TotalCargaKilos")] Importacion importacion)
    {
        if (ModelState.IsValid)
        {
            importacion.IdUsuario = HttpContext.User.GetUserId();
            importacion.FechaHoraSystema = DateTime.Now;
            _context.Add(importacion);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Barcos = new SelectList(_context.Barcos, "Id", "NombreBarco", importacion.IdBarco);
        return View(importacion);
    }

    // GET: Importacion/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var importacion = await _context.Importaciones.FindAsync(id);
        if (importacion == null)
        {
            return NotFound();
        }
        ViewBag.Barcos = new SelectList(_context.Barcos, "Id", "NombreBarco", importacion.IdBarco);
        return View(importacion);
    }

    // POST: Importacion/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,FechaHora,IdBarco,TotalCargaKilos,FechaHoraSystema")] Importacion importacion)
    {
        if (id != importacion.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                importacion.IdUsuario = HttpContext.User.GetUserId();
                _context.Update(importacion);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ImportacionExists(importacion.Id))
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
        ViewBag.Barcos = new SelectList(_context.Barcos, "Id", "NombreBarco", importacion.IdBarco);
        return View(importacion);
    }

    // GET: Importacion/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var importacion = await _context.Importaciones
            .Include(i => i.Barco)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (importacion == null)
        {
            return NotFound();
        }

        return View(importacion);
    }

    // POST: Importacion/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var importacion = await _context.Importaciones.FindAsync(id);
        if (importacion != null)
        {
            _context.Importaciones.Remove(importacion);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool ImportacionExists(int id)
    {
        return _context.Importaciones.Any(e => e.Id == id);
    }
}