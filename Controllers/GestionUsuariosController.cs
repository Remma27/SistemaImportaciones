using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using Sistema_de_Gestion_de_Importaciones.ViewModels;
using API.Models;
using Sistema_de_Gestion_de_Importaciones.Extensions;

namespace Sistema_de_Gestion_de_Importaciones.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class GestionUsuariosController : Controller
    {
        private readonly IUsuarioService _usuarioService;
        private readonly ILogger<GestionUsuariosController> _logger;

        public GestionUsuariosController(
            IUsuarioService usuarioService,
            ILogger<GestionUsuariosController> logger)
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
                _logger.LogError(ex, "Error al obtener la lista de usuarios");
                this.Error("Error al cargar los usuarios: " + ex.Message);
                return View(new List<Usuario>());
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var usuario = await _usuarioService.GetByIdAsync(id);
                if (usuario == null)
                {
                    this.Error("Usuario no encontrado");
                    return RedirectToAction(nameof(Index));
                }
                return View(usuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener detalles del usuario {Id}", id);
                this.Error("Error al cargar el usuario: " + ex.Message);
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
            try
            {
                if (ModelState.IsValid)
                {
                    usuario.fecha_creacion = DateTime.Now;
                    usuario.activo = true;
                    await _usuarioService.CreateAsync(usuario);
                    this.Success("Usuario creado correctamente");
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el usuario");
                this.Error("Error al crear el usuario: " + ex.Message);
                ModelState.AddModelError("", "Error al crear el usuario: " + ex.Message);
            }
            return View(usuario);
        }

        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var usuario = await _usuarioService.GetByIdAsync(id);
                if (usuario == null)
                {
                    this.Error("Usuario no encontrado");
                    return RedirectToAction(nameof(Index));
                }
                return View(usuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el usuario {Id} para editar", id);
                this.Error("Error al cargar el usuario: " + ex.Message);
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

            try
            {
                if (ModelState.IsValid)
                {
                    // Obtener el usuario actual para preservar datos importantes
                    var usuarioActual = await _usuarioService.GetByIdAsync(id);
                    if (usuarioActual != null)
                    {
                        // Preservar valores que no deben cambiar en la edición básica
                        usuario.password_hash = usuarioActual.password_hash; // Mantener contraseña actual
                        usuario.rol_id = usuarioActual.rol_id; // Mantener rol actual
                        usuario.fecha_creacion = usuarioActual.fecha_creacion; // Mantener fecha de creación
                        usuario.ultimo_acceso = usuarioActual.ultimo_acceso; // Mantener último acceso
                        
                        _logger.LogInformation($"Actualizando usuario {id} - Email:{usuario.email}, Nombre:{usuario.nombre}, Rol:{usuario.rol_id}");
                    }
                    
                    await _usuarioService.UpdateAsync(id, usuario);
                    this.Success("Usuario actualizado correctamente");
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el usuario {Id}", id);
                this.Error("Error al actualizar el usuario: " + ex.Message);
                ModelState.AddModelError("", "Error al actualizar: " + ex.Message);
            }
            return View(usuario);
        }

        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var usuario = await _usuarioService.GetByIdAsync(id);
                if (usuario == null)
                {
                    this.Error("Usuario no encontrado");
                    return RedirectToAction(nameof(Index));
                }
                return View(usuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el usuario {Id} para eliminar", id);
                this.Error("Error al cargar el usuario: " + ex.Message);
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el usuario {Id}", id);
                this.Error("Error al eliminar el usuario: " + ex.Message);
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> AsignarRol(int id)
        {
            try
            {
                var usuario = await _usuarioService.GetByIdAsync(id);
                if (usuario == null)
                {
                    this.Error("Usuario no encontrado");
                    return RedirectToAction(nameof(Index));
                }

                // Obtener roles directamente y crear el SelectList manualmente
                var roles = await _usuarioService.GetRolesAsync();
                _logger.LogInformation($"[ROLES] Obtenidos {roles.Count} roles para el usuario {id}");
                
                foreach (var rol in roles)
                {
                    _logger.LogInformation($"Rol obtenido: ID={rol.id}, Nombre={rol.nombre}");
                }
                
                // Crear SelectList manual con los roles obtenidos
                var selectListItems = roles.Select(r => new SelectListItem
                {
                    Value = r.id.ToString(),
                    Text = r.nombre,
                    Selected = usuario.rol_id == r.id
                }).ToList();
                
                ViewBag.Roles = new SelectList(selectListItems, "Value", "Text");
                ViewBag.Usuario = usuario;
                
                var viewModel = new AsignarRolViewModel
                {
                    UsuarioId = usuario.id,
                    RolId = usuario.rol_id ?? 0
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al preparar asignación de rol para usuario {Id}", id);
                this.Error("Error al cargar los datos: " + ex.Message);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AsignarRol(AsignarRolViewModel viewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var result = await _usuarioService.AsignarRolAsync(viewModel.UsuarioId, viewModel.RolId);
                    
                    if (result.Success)
                    {
                        this.Success("Rol asignado correctamente");
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        this.Error($"Error al asignar el rol: {result.ErrorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al asignar rol al usuario {Id}", viewModel.UsuarioId);
                this.Error("Error al asignar el rol: " + ex.Message);
                ModelState.AddModelError("", "Error al asignar el rol: " + ex.Message);
            }

            // Si llegamos aquí es porque hubo un error, volvemos a cargar el formulario
            var usuario = await _usuarioService.GetByIdAsync(viewModel.UsuarioId);
            var roles = await _usuarioService.GetRolesAsync();
            
            ViewBag.Roles = new SelectList(roles, "id", "nombre", viewModel.RolId);
            ViewBag.Usuario = usuario;
            
            return View(viewModel);
        }

        public async Task<IActionResult> CambiarPassword(int id)
        {
            try
            {
                var usuario = await _usuarioService.GetByIdAsync(id);
                if (usuario == null)
                {
                    this.Error("Usuario no encontrado");
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = new CambiarPasswordViewModel
                {
                    UsuarioId = usuario.id,
                    NombreUsuario = usuario.nombre,
                    Email = usuario.email
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al preparar cambio de contraseña para usuario {Id}", id);
                this.Error("Error al cargar los datos: " + ex.Message);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarPassword(CambiarPasswordViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            try
            {
                var result = await _usuarioService.CambiarPasswordAsync(viewModel.UsuarioId, viewModel.Password);
                
                if (result.Success)
                {
                    this.Success("Contraseña cambiada correctamente");
                    return RedirectToAction(nameof(Index));
                }
                
                this.Error($"Error al cambiar la contraseña: {result.ErrorMessage}");
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar contraseña del usuario {Id}", viewModel.UsuarioId);
                this.Error("Error al cambiar la contraseña: " + ex.Message);
                ModelState.AddModelError("", "Error al cambiar la contraseña: " + ex.Message);
                return View(viewModel);
            }
        }

        public async Task<IActionResult> ToggleActivo(int id)
        {
            try
            {
                var usuario = await _usuarioService.GetByIdAsync(id);
                if (usuario == null)
                {
                    this.Error("Usuario no encontrado");
                    return RedirectToAction(nameof(Index));
                }

                // Invertir el estado actual
                bool nuevoEstado = !(usuario.activo ?? true);
                
                // Usar el método específico para cambiar estado
                var result = await _usuarioService.ToggleActivoAsync(id, nuevoEstado);
                
                if (result.Success)
                {
                    string mensaje = nuevoEstado ? "activado" : "desactivado";
                    this.Success($"Usuario {mensaje} correctamente");
                }
                else
                {
                    this.Error($"Error al cambiar estado del usuario: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado activo del usuario {Id}", id);
                this.Error("Error al cambiar estado del usuario: " + ex.Message);
            }
            
            return RedirectToAction(nameof(Index));
        }
    }
}
