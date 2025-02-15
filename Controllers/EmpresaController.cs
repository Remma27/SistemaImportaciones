using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_de_Gestion_de_Importaciones.Models;
using Sistema_de_Gestion_de_Importaciones.Data;
using Sistema_de_Gestion_de_Importaciones.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace Sistema_de_Gestion_de_Importaciones.Controllers;

public class EmpresaController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EmpresaController> _logger;

    public EmpresaController(ApplicationDbContext context, ILogger<EmpresaController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Empresa
    [Authorize]
    public async Task<IActionResult> Index()
    {
        return View(await _context.Empresas.ToListAsync());
    }

    // GET: Empresa/Details/5
    [Authorize]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var empresa = await _context.Empresas
            .FirstOrDefaultAsync(m => m.IdEmpresa == id);
        if (empresa == null)
        {
            return NotFound();
        }

        return View(empresa);
    }

    // GET: Empresa/Create
    [Authorize]
    public IActionResult Create()
    {
        return View();
    }

    // POST: Empresa/Create
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("NombreEmpresa,Estatus")] Empresa empresa)
    {
        if (ModelState.IsValid)
        {
            // Set idusuario using the logged-in user's id from claims:
            empresa.IdUsuario = HttpContext.User.GetUserId();
            _context.Add(empresa);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(empresa);
    }

    // GET: Empresa/Edit/5
    [Authorize]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var empresa = await _context.Empresas.FindAsync(id);
        if (empresa == null)
        {
            return NotFound();
        }
        return View(empresa);
    }

    // POST: Empresa/Edit/5
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("IdEmpresa,NombreEmpresa,Estatus")] Empresa empresa)
    {
        if (id != empresa.IdEmpresa)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                // Update user tracker property:
                empresa.IdUsuario = HttpContext.User.GetUserId();
                _context.Update(empresa);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EmpresaExists(empresa.IdEmpresa))
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
        return View(empresa);
    }

    // GET: Empresa/Delete/5
    [Authorize]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var empresa = await _context.Empresas
            .FirstOrDefaultAsync(m => m.IdEmpresa == id);
        if (empresa == null)
        {
            return NotFound();
        }

        return View(empresa);
    }

    // POST: Empresa/Delete/5
    [Authorize]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var empresa = await _context.Empresas.FindAsync(id);
        if (empresa != null)
        {
            _context.Empresas.Remove(empresa);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool EmpresaExists(int id)
    {
        return _context.Empresas.Any(e => e.IdEmpresa == id);
    }
}