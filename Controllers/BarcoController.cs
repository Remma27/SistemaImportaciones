using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_de_Gestion_de_Importaciones.Models;
using Sistema_de_Gestion_de_Importaciones.Data;
namespace Sistema_de_Gestion_de_Importaciones.Controllers;

public class BarcoController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BarcoController> _logger;

    public BarcoController(ApplicationDbContext context, ILogger<BarcoController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Barco
    public async Task<IActionResult> Index()
    {
        return View(await _context.Barcos.ToListAsync());
    }

    // GET: Barco/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var barco = await _context.Barcos
            .FirstOrDefaultAsync(m => m.Id == id);
        if (barco == null)
        {
            return NotFound();
        }

        return View(barco);
    }

    // GET: Barco/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Barco/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("NombreBarco,Escotilla1,Escotilla2,Escotilla3,Escotilla4,Escotilla5,Escotilla6,Escotilla7")] Barco barco)
    {
        if (ModelState.IsValid)
        {
            _context.Add(barco);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(barco);
    }

    // GET: Barco/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var barco = await _context.Barcos.FindAsync(id);
        if (barco == null)
        {
            return NotFound();
        }
        return View(barco);
    }

    // POST: Barco/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,NombreBarco,Escotilla1,Escotilla2,Escotilla3,Escotilla4,Escotilla5,Escotilla6,Escotilla7")] Barco barco)
    {
        if (id != barco.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(barco);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BarcoExists(barco.Id))
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
        return View(barco);
    }

    // GET: Barco/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var barco = await _context.Barcos
            .FirstOrDefaultAsync(m => m.Id == id);
        if (barco == null)
        {
            return NotFound();
        }

        return View(barco);
    }

    // POST: Barco/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var barco = await _context.Barcos.FindAsync(id);
        if (barco != null)
        {
            _context.Barcos.Remove(barco);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool BarcoExists(int id)
    {
        return _context.Barcos.Any(e => e.Id == id);
    }
}