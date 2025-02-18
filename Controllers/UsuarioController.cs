using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sistema_de_Gestion_de_Importaciones.Models;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace Sistema_de_Gestion_de_Importaciones.Controllers
{
    //[Authorize] // Opcional: Solo usuarios autenticados pueden acceder.
    public class UsuarioController : Controller
    {
        private readonly IUsuarioService _usuarioService;
        private readonly ILogger<UsuarioController> _logger;

        public UsuarioController(IUsuarioService usuarioService, ILogger<UsuarioController> logger)
        {
            _usuarioService = usuarioService;
            _logger = logger;
        }

        // GET: Usuario
        public async Task<IActionResult> Index()
        {
            var usuarios = await _usuarioService.GetAllAsync();
            return View(usuarios);
        }

        // GET: Usuario/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var usuario = await _usuarioService.GetByIdAsync(id.Value);
            if (usuario == null)
                return NotFound();

            return View(usuario);
        }

        // GET: Usuario/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Usuario/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Usuario usuario)
        {
            if (ModelState.IsValid)
            {
                usuario.fecha_creacion = DateTime.Now; // Establece la fecha de creación
                await _usuarioService.CreateAsync(usuario);
                return RedirectToAction(nameof(Index));
            }
            return View(usuario);
        }

        // GET: Usuario/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var usuario = await _usuarioService.GetByIdAsync(id.Value);
            if (usuario == null)
                return NotFound();

            return View(usuario);
        }

        // POST: Usuario/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Usuario usuario)
        {
            if (id != usuario.id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    await _usuarioService.UpdateAsync(id, usuario);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al actualizar el usuario con ID '{Id}'", id);
                    if (!await UsuarioExists(usuario.id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(usuario);
        }

        // GET: Usuario/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var usuario = await _usuarioService.GetByIdAsync(id.Value);
            if (usuario == null)
                return NotFound();

            return View(usuario);
        }

        // POST: Usuario/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Realizamos un soft delete (se marca como inactivo)
            await _usuarioService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> UsuarioExists(int id)
        {
            var usuario = await _usuarioService.GetByIdAsync(id);
            return usuario != null;
        }
    }
}