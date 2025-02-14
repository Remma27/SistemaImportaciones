using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_de_Gestion_de_Importaciones.Models;
using Sistema_de_Gestion_de_Importaciones.Data;
using Sistema_de_Gestion_de_Importaciones.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sistema_de_Gestion_de_Importaciones.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace Sistema_de_Gestion_de_Importaciones.Controllers;

public class RegistroRequerimientosController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RegistroRequerimientosController> _logger;

    public RegistroRequerimientosController(ApplicationDbContext context, ILogger<RegistroRequerimientosController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: RegistroRequerimientos
    [Authorize]
    public async Task<IActionResult> Index(int? selectedBarco)
    {
        try
        {
            _logger.LogInformation("Starting data retrieval for RegistroRequerimientos");

            // Obtener la lista de barcos que tienen movimientos (TipoTransaccion == 1)
            var barcos = await (from b in _context.Barcos
                                join i in _context.Importaciones on b.Id equals i.IdBarco
                                join m in _context.Movimientos on i.Id equals m.IdImportacion
                                where m.TipoTransaccion == 1
                                select b)
                               .Distinct()
                               .ToListAsync();
            ViewBag.Barcos = new SelectList(barcos, "Id", "NombreBarco", selectedBarco);

            // Si no se ha seleccionado un barco, no retornar nada
            if (!selectedBarco.HasValue)
            {
                return View(new List<RegistroRequerimientosViewModel>());
            }

            var result = await (from m in _context.Movimientos
                                join i in _context.Importaciones on m.IdImportacion equals i.Id
                                join e in _context.Empresas on m.IdEmpresa equals e.IdEmpresa
                                join b in _context.Barcos on i.IdBarco equals b.Id
                                where b.Id == selectedBarco.Value && m.TipoTransaccion == 1
                                select new RegistroRequerimientosViewModel
                                {
                                    IdMovimiento = m.Id,
                                    FechaHora = m.FechaHora,
                                    IdImportacion = m.IdImportacion,
                                    Importacion = b.NombreBarco ?? string.Empty,
                                    IdEmpresa = m.IdEmpresa,
                                    Empresa = e.NombreEmpresa ?? string.Empty,
                                    TipoTransaccion = m.TipoTransaccion ?? 0,
                                    CantidadRequerida = m.CantidadRequerida ?? 0,
                                    CantidadCamiones = m.CantidadCamiones ?? 0
                                }).ToListAsync();

            _logger.LogInformation("Data retrieval successful, returning view with {Count} items", result.Count);

            return View(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading registro requerimientos: {Message}", ex.Message);
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // GET: RegistroRequerimientos/Details/5
    [Authorize]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        try
        {
            var movimiento = await _context.Movimientos
                .Include(m => m.Empresa)
                .Include(m => m.Importacion!)
                    .ThenInclude(i => i.Barco)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movimiento == null) return NotFound();

            var viewModel = new RegistroRequerimientosViewModel
            {
                IdMovimiento = movimiento.Id,
                FechaHora = movimiento.FechaHora,
                IdImportacion = movimiento.IdImportacion,
                Importacion = movimiento.Importacion?.Barco?.NombreBarco ?? string.Empty,
                IdEmpresa = movimiento.IdEmpresa,
                Empresa = movimiento.Empresa?.NombreEmpresa ?? string.Empty,
                TipoTransaccion = movimiento.TipoTransaccion,
                CantidadRequerida = movimiento.CantidadRequerida,
                CantidadCamiones = movimiento.CantidadCamiones
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading registro requerimientos details: {Message}", ex.Message);
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // GET: RegistroRequerimientos/Create
    [Authorize]
    public IActionResult Create()
    {
        try
        {
            var importaciones = (from i in _context.Importaciones
                                 join b in _context.Barcos on i.IdBarco equals b.Id
                                 select new { i.Id, b.NombreBarco }).ToList();

            // Filtrar empresas con estatus "Activo"
            ViewBag.Importaciones = new SelectList(importaciones, "Id", "NombreBarco");
            ViewBag.Empresas = new SelectList(_context.Empresas.Where(e => e.Estatus == 1), "IdEmpresa", "NombreEmpresa");

            var viewModel = new RegistroRequerimientosViewModel
            {
                FechaHora = DateTime.Now
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading create form for registro requerimientos: {Message}", ex.Message);
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // POST: RegistroRequerimientos/Create
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("FechaHora,IdImportacion,IdEmpresa,TipoTransaccion,CantidadRequerida,CantidadCamiones")] RegistroRequerimientosViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var movimiento = new Movimiento
                {
                    FechaHora = viewModel.FechaHora,
                    IdImportacion = viewModel.IdImportacion,
                    IdEmpresa = viewModel.IdEmpresa,
                    TipoTransaccion = viewModel.TipoTransaccion,
                    CantidadRequerida = viewModel.CantidadRequerida,
                    CantidadCamiones = viewModel.CantidadCamiones
                };

                movimiento.IdUsuario = HttpContext.User.GetUserId();
                _context.Add(movimiento);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating registro requerimientos: {Message}", ex.Message);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        var importaciones = (from i in _context.Importaciones
                             join b in _context.Barcos on i.IdBarco equals b.Id
                             select new { i.Id, b.NombreBarco }).ToList();

        ViewBag.Importaciones = new SelectList(importaciones, "Id", "NombreBarco", viewModel.IdImportacion);
        // Asegurar filtrar empresas activas
        ViewBag.Empresas = new SelectList(_context.Empresas.Where(e => e.Estatus == 1), "IdEmpresa", "NombreEmpresa", viewModel.IdEmpresa);
        return View(viewModel);
    }

    // GET: RegistroRequerimientos/Edit/5
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        try
        {
            var result = await (from m in _context.Movimientos
                                join i in _context.Importaciones on m.IdImportacion equals i.Id
                                join e in _context.Empresas on m.IdEmpresa equals e.IdEmpresa
                                join b in _context.Barcos on i.IdBarco equals b.Id
                                where m.Id == id
                                select new RegistroRequerimientosViewModel
                                {
                                    IdMovimiento = m.Id,
                                    FechaHora = m.FechaHora,
                                    IdImportacion = m.IdImportacion,
                                    Importacion = b.NombreBarco ?? string.Empty,
                                    IdEmpresa = m.IdEmpresa,
                                    Empresa = e.NombreEmpresa ?? string.Empty,
                                    TipoTransaccion = m.TipoTransaccion ?? 0,
                                    CantidadRequerida = m.CantidadRequerida ?? 0,
                                    CantidadCamiones = m.CantidadCamiones ?? 0
                                }).FirstOrDefaultAsync();

            if (result == null)
            {
                return NotFound();
            }

            var importaciones = (from i in _context.Importaciones
                                 join b in _context.Barcos on i.IdBarco equals b.Id
                                 select new { i.Id, b.NombreBarco }).ToList();

            ViewBag.Importaciones = new SelectList(importaciones, "Id", "NombreBarco", result.IdImportacion);
            // Filtrar empresas con estatus "Activo"
            ViewBag.Empresas = new SelectList(_context.Empresas.Where(e => e.Estatus == 1), "IdEmpresa", "NombreEmpresa", result.IdEmpresa);

            return View(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading edit form for movimiento ID {Id}: {Message}", id, ex.Message);
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: RegistroRequerimientos/Edit/5
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("[controller]/Edit/{id}")]
    public async Task<IActionResult> Edit(int id, [Bind("IdMovimiento,FechaHora,IdImportacion,IdEmpresa,TipoTransaccion,CantidadRequerida,CantidadCamiones")] RegistroRequerimientosViewModel viewModel)
    {
        if (id != viewModel.IdMovimiento)
        {
            _logger.LogWarning("ID mismatch in Edit Post. URL ID: {UrlId}, Model ID: {ModelId}", id, viewModel.IdMovimiento);
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var movimiento = await _context.Movimientos.FindAsync(id);
                if (movimiento == null)
                {
                    _logger.LogWarning("Movimiento not found during edit. ID: {Id}", id);
                    return NotFound();
                }

                // Actualizar las propiedades
                movimiento.FechaHora = viewModel.FechaHora;
                movimiento.IdImportacion = viewModel.IdImportacion;
                movimiento.IdEmpresa = viewModel.IdEmpresa;
                movimiento.TipoTransaccion = viewModel.TipoTransaccion;
                movimiento.CantidadRequerida = viewModel.CantidadRequerida;
                movimiento.CantidadCamiones = viewModel.CantidadCamiones;

                // Asignar ID de usuario
                movimiento.IdUsuario = HttpContext.User.GetUserId();

                // La entidad ya está siendo rastreada, solo se guarda los cambios
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully updated movimiento ID: {Id}", id);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error updating movimiento ID {Id}", id);
                if (!await MovimientoExists(viewModel.IdMovimiento))
                {
                    return NotFound();
                }
                ModelState.AddModelError("", "Error de concurrencia al actualizar el movimiento.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating movimiento ID {Id}: {Message}", id, ex.Message);
                ModelState.AddModelError("", "Ha ocurrido un error al guardar los cambios.");
            }
        }

        // Si llega aquí, algo falló; recargar los selectlists y volver a mostrar el formulario
        try
        {
            var importaciones = await (from i in _context.Importaciones
                                       join b in _context.Barcos on i.IdBarco equals b.Id
                                       where b != null
                                       select new { i.Id, NombreBarco = b.NombreBarco ?? "Sin nombre" })
                                     .ToListAsync();

            ViewBag.Importaciones = new SelectList(importaciones, "Id", "NombreBarco", viewModel.IdImportacion);
            ViewBag.Empresas = new SelectList(_context.Empresas.Where(e => e.Estatus == 1), "IdEmpresa", "NombreEmpresa", viewModel.IdEmpresa);

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading edit form data: {Message}", ex.Message);
            return RedirectToAction(nameof(Index));
        }
    }

    // GET: RegistroRequerimientos/Delete/5
    [Authorize]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var result = await (from m in _context.Movimientos
                            join i in _context.Importaciones on m.IdImportacion equals i.Id
                            join e in _context.Empresas on m.IdEmpresa equals e.IdEmpresa
                            join b in _context.Barcos on i.IdBarco equals b.Id
                            where m.Id == id
                            select new RegistroRequerimientosViewModel
                            {
                                IdMovimiento = m.Id,
                                FechaHora = m.FechaHora,
                                IdImportacion = m.IdImportacion,
                                Importacion = b.NombreBarco ?? string.Empty,
                                IdEmpresa = m.IdEmpresa,
                                Empresa = e.NombreEmpresa ?? string.Empty,
                                TipoTransaccion = m.TipoTransaccion ?? 0,
                                CantidadRequerida = m.CantidadRequerida ?? 0,
                                CantidadCamiones = m.CantidadCamiones ?? 0
                            }).FirstOrDefaultAsync();

        if (result == null)
        {
            return NotFound();
        }

        return View(result);
    }

    // POST: RegistroRequerimientos/Delete/5
    [Authorize]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var movimiento = await _context.Movimientos.FindAsync(id);
        if (movimiento != null)
        {
            _context.Movimientos.Remove(movimiento);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> MovimientoExists(int id)
    {
        return await _context.Movimientos.AnyAsync(e => e.Id == id);
    }
}



