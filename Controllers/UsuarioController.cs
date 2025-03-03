using Microsoft.AspNetCore.Mvc;
using API.Models;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;

using Sistema_de_Gestion_de_Importaciones.Extensions;

namespace Sistema_de_Gestion_de_Importaciones.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly IUsuarioService _usuarioService;
        private readonly ILogger<UsuarioController> _logger;

        public UsuarioController(IUsuarioService usuarioService, ILogger<UsuarioController> logger)
        {
            _usuarioService = usuarioService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var usuarios = await _usuarioService.GetAllAsync();
                return View(usuarios);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los usuarios");
                this.Error("Error al cargar los usuarios. Por favor, intente más tarde.");
                return View(Array.Empty<Usuario>());
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                this.Error("ID de usuario no especificado");
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var usuario = await _usuarioService.GetByIdAsync(id.Value);
                if (usuario == null)
                {
                    this.Error($"Usuario con ID {id} no encontrado");
                    return RedirectToAction(nameof(Index));
                }

                return View(usuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el usuario {Id}", id);
                this.Error("Error al cargar los datos del usuario.");
                return RedirectToAction(nameof(Index));
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Usuario usuario)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    usuario.fecha_creacion = DateTime.Now;
                    await _usuarioService.CreateAsync(usuario);
                    this.Success("Usuario creado correctamente");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al crear usuario");
                    this.Error("Error al crear el usuario. Por favor, intente más tarde.");
                    ModelState.AddModelError("", "Error al crear el usuario: " + ex.Message);
                }
            }
            return View(usuario);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                this.Error("ID de usuario no especificado");
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var usuario = await _usuarioService.GetByIdAsync(id.Value);
                if (usuario == null)
                {
                    this.Error($"Usuario con ID {id} no encontrado");
                    return RedirectToAction(nameof(Index));
                }

                return View(usuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el usuario {Id}", id);
                this.Error("Error al cargar los datos del usuario.");
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Usuario usuario)
        {
            if (id != usuario.id)
            {
                this.Error("ID de usuario no coincide");
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _usuarioService.UpdateAsync(id, usuario);
                    this.Success("Usuario actualizado correctamente");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al actualizar el usuario con ID '{Id}'", id);
                    if (!await UsuarioExists(usuario.id))
                    {
                        this.Error($"Usuario con ID {id} no encontrado");
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        ModelState.AddModelError("", "Error al actualizar: " + ex.Message);
                    }
                }
            }
            return View(usuario);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                this.Error("ID de usuario no especificado");
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var usuario = await _usuarioService.GetByIdAsync(id.Value);
                if (usuario == null)
                {
                    this.Error($"Usuario con ID {id} no encontrado");
                    return RedirectToAction(nameof(Index));
                }

                return View(usuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el usuario {Id}", id);
                this.Error("Error al cargar los datos del usuario.");
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _usuarioService.DeleteAsync(id);
                this.Success("Usuario eliminado correctamente");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el usuario con ID {Id}", id);
                this.Error("Error al eliminar el usuario: " + ex.Message);
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task<bool> UsuarioExists(int id)
        {
            var usuario = await _usuarioService.GetByIdAsync(id);
            return usuario != null;
        }
    }
}