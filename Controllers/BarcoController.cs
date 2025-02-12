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
        try
        {
            var barcos = await _context.Barcos
                .AsNoTracking()
                .ToListAsync();

            return View(barcos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving barcos");
            return View(new List<Barco>());
        }
    }

    // GET: Barco/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        try
        {
            var barco = await _context.Barcos
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (barco == null) return NotFound();

            // Obtener el conteo de importaciones relacionadas
            var importacionesCount = await _context.Importaciones
                .Where(i => i.IdBarco == id)
                .CountAsync();

            ViewBag.ImportacionesCount = importacionesCount;

            return View(barco);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving barco details for ID: {Id}", id);
            return NotFound();
        }
    }

    // GET: Barco/Create
    public IActionResult Create()
    {
        var barco = new Barco
        {
            Escotilla1 = 0.0m,
            Escotilla2 = 0.0m,
            Escotilla3 = 0.0m,
            Escotilla4 = 0.0m,
            Escotilla5 = 0.0m,
            Escotilla6 = 0.0m,
            Escotilla7 = 0.0m
        };
        return View(barco);
    }

    // POST: Barco/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("NombreBarco,Escotilla1,Escotilla2,Escotilla3,Escotilla4,Escotilla5,Escotilla6,Escotilla7")] Barco barco)
    {
        try
        {
            if (ModelState.IsValid)
            {
                _context.Add(barco);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created new barco: {NombreBarco}", barco.NombreBarco);
                return RedirectToAction(nameof(Index));
            }
            return View(barco);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating barco: {NombreBarco}", barco.NombreBarco);
            ModelState.AddModelError("", "Error al crear el barco. Por favor intente nuevamente.");
            return View(barco);
        }
    }

    // GET: Barco/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        try
        {
            var barco = await _context.Barcos.FindAsync(id);
            if (barco == null)
            {
                return NotFound();
            }
            return View(barco);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving barco for edit, ID: {Id}", id);
            return NotFound();
        }
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

        try
        {
            if (ModelState.IsValid)
            {
                _context.Update(barco);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated barco: {NombreBarco}", barco.NombreBarco);
                return RedirectToAction(nameof(Index));
            }
            return View(barco);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            if (!await BarcoExists(barco.Id))
            {
                return NotFound();
            }
            _logger.LogError(ex, "Concurrency error updating barco: {Id}", id);
            ModelState.AddModelError("", "Error al actualizar el barco. Por favor intente nuevamente.");
            return View(barco);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating barco: {Id}", id);
            ModelState.AddModelError("", "Error al actualizar el barco. Por favor intente nuevamente.");
            return View(barco);
        }
    }

    // GET: Barco/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        try
        {
            var barco = await _context.Barcos
                .FirstOrDefaultAsync(m => m.Id == id);

            if (barco == null)
            {
                return NotFound();
            }

            return View(barco);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving barco for delete, ID: {Id}", id);
            return NotFound();
        }
    }

    // POST: Barco/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            // Verificar si hay importaciones que usan este barco
            var hasImportaciones = await _context.Importaciones
                .AnyAsync(i => i.IdBarco == id);

            if (hasImportaciones)
            {
                ModelState.AddModelError("", "No se puede eliminar el barco porque tiene importaciones asociadas.");
                var barco = await _context.Barcos.FindAsync(id);
                return View("Delete", barco);
            }

            var barcoToDelete = await _context.Barcos.FindAsync(id);
            if (barcoToDelete != null)
            {
                _context.Barcos.Remove(barcoToDelete);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted barco: {NombreBarco}", barcoToDelete.NombreBarco);
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting barco: {Id}", id);
            return RedirectToAction(nameof(Index));
        }
    }

    private async Task<bool> BarcoExists(int id)
    {
        return await _context.Barcos.AnyAsync(e => e.Id == id);
    }
}