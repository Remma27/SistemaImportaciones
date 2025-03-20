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
                    // En lugar de usar CreateAsync, usar RegistrarUsuarioAsync que sabemos que funciona
                    var viewModel = new RegistroViewModel
                    {
                        Nombre = usuario.nombre ?? string.Empty,
                        Email = usuario.email ?? string.Empty,
                        Password = usuario.password_hash ?? string.Empty,
                        ConfirmPassword = usuario.password_hash ?? string.Empty // Añadir ConfirmPassword para cumplir con el requisito
                    };
                    
                    var result = await _usuarioService.RegistrarUsuarioAsync(viewModel);
                    
                    if (result.Success)
                    {
                        this.Success("Usuario creado correctamente");
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        this.Error($"Error al crear el usuario: {result.ErrorMessage}");
                        ModelState.AddModelError("", result.ErrorMessage ?? "Error desconocido al crear el usuario");
                    }
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
            // Añadir registro exhaustivo para diagnosticar el problema
            _logger.LogWarning("Iniciando edición de usuario {Id} - POST recibido con datos: {@Usuario}", id, usuario);
            
            if (id != usuario.id)
            {
                _logger.LogWarning("ID de usuario no coincide: recibido {Id} vs usuario.id {UsuarioId}", id, usuario.id);
                this.Error("ID de usuario no coincide");
                return RedirectToAction(nameof(Index));
            }
            
            try
            {
                // Verificar el modelo sin ModelState
                if (string.IsNullOrEmpty(usuario.nombre) || string.IsNullOrEmpty(usuario.email))
                {
                    _logger.LogWarning("Datos inválidos: Nombre={Nombre}, Email={Email}", usuario.nombre, usuario.email);
                    this.Error("Los campos nombre y email son obligatorios");
                    return View(usuario);
                }
                
                // Obtener el usuario actual para preservar datos importantes
                var usuarioActual = await _usuarioService.GetByIdAsync(id);
                if (usuarioActual == null)
                {
                    _logger.LogWarning("No se encontró el usuario con ID {Id} al intentar editarlo", id);
                    this.Error("Usuario no encontrado");
                    return RedirectToAction(nameof(Index));
                }
                
                _logger.LogInformation("Usuario actual encontrado: {@UsuarioActual}", new { 
                    usuarioActual.id, 
                    usuarioActual.nombre, 
                    usuarioActual.email, 
                    RolId = usuarioActual.rol_id
                });
                
                // Preservar valores que no deben cambiar en la edición básica
                usuario.password_hash = usuarioActual.password_hash; // Mantener contraseña actual
                usuario.rol_id = usuarioActual.rol_id; // Mantener rol actual
                usuario.fecha_creacion = usuarioActual.fecha_creacion; // Mantener fecha de creación
                usuario.ultimo_acceso = usuarioActual.ultimo_acceso; // Mantener último acceso
                
                _logger.LogInformation("Preparado usuario para actualizar: {@Usuario}", new {
                    usuario.id,
                    usuario.nombre,
                    usuario.email,
                    usuario.activo,
                    RolId = usuario.rol_id
                });
                
                // Registrar datos a enviar
                _logger.LogInformation("Enviando actualización para usuario {Id}: {Nombre}, {Email}, {Activo}", 
                    id, usuario.nombre, usuario.email, usuario.activo);
                
                // USAR EL SERVICIO PARA ACTUALIZAR - con try/catch específico
                try
                {
                    var usuarioActualizado = await _usuarioService.UpdateAsync(id, usuario);
                    
                    _logger.LogInformation("Usuario actualizado exitosamente. Datos devueltos: {@Usuario}", 
                        new { usuarioActualizado.id, usuarioActualizado.nombre, usuarioActualizado.email });
                    
                    // Agregar mensaje de éxito y redireccionar
                    this.Success("Usuario actualizado correctamente");
                    
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception updateEx)
                {
                    _logger.LogError(updateEx, "Error específico al llamar UpdateAsync para usuario {Id}", id);
                    this.Error($"Error al actualizar el usuario: {updateEx.Message}");
                    ModelState.AddModelError("", $"Error al actualizar: {updateEx.Message}");
                    return View(usuario);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error general al actualizar el usuario {Id}", id);
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

                var roles = await _usuarioService.GetRolesAsync();
                
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
                // Añadir diagnóstico detallado
                _logger.LogInformation("Iniciando asignación de rol - UsuarioId: {UsuarioId}, RolId: {RolId}", 
                    viewModel.UsuarioId, viewModel.RolId);
                
                if (viewModel.UsuarioId <= 0)
                {
                    ModelState.AddModelError("", "ID de usuario inválido");
                    _logger.LogWarning("ID de usuario inválido: {UsuarioId}", viewModel.UsuarioId);
                }
                
                if (viewModel.RolId <= 0)
                {
                    ModelState.AddModelError("", "Debe seleccionar un rol válido");
                    _logger.LogWarning("ID de rol inválido: {RolId}", viewModel.RolId);
                }
                
                if (ModelState.IsValid)
                {
                    _logger.LogInformation("Modelo válido, llamando a AsignarRolAsync...");
                    var result = await _usuarioService.AsignarRolAsync(viewModel.UsuarioId, viewModel.RolId);
                    
                    if (result.Success)
                    {
                        _logger.LogInformation("Rol asignado correctamente al usuario {UsuarioId}", viewModel.UsuarioId);
                        this.Success("Rol asignado correctamente");
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        _logger.LogWarning("Error al asignar rol: {ErrorMessage}", result.ErrorMessage);
                        this.Error($"Error al asignar el rol: {result.ErrorMessage}");
                        ModelState.AddModelError("", $"Error al asignar el rol: {result.ErrorMessage}");
                    }
                }
                else
                {
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        _logger.LogWarning("Error de validación: {ErrorMessage}", error.ErrorMessage);
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
            
            var selectListItems = roles.Select(r => new SelectListItem
            {
                Value = r.id.ToString(),
                Text = r.nombre,
                Selected = viewModel.RolId == r.id
            }).ToList();
            
            ViewBag.Roles = new SelectList(selectListItems, "Value", "Text");
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
                _logger.LogWarning("Modelo inválido al cambiar contraseña para usuario {Id}. Errores: {@Errors}",
                    viewModel.UsuarioId,
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return View(viewModel);
            }

            try
            {
                _logger.LogInformation("Intentando cambiar contraseña para usuario ID: {UserId}", viewModel.UsuarioId);
                
                // Validaciones adicionales
                if (string.IsNullOrEmpty(viewModel.Password))
                {
                    _logger.LogWarning("Contraseña vacía al intentar cambiar para usuario {Id}", viewModel.UsuarioId);
                    this.Error("La contraseña no puede estar vacía");
                    return View(viewModel);
                }
                
                if (viewModel.Password != viewModel.ConfirmPassword)
                {
                    _logger.LogWarning("Las contraseñas no coinciden al cambiar para usuario {Id}", viewModel.UsuarioId);
                    this.Error("Las contraseñas no coinciden");
                    return View(viewModel);
                }
                
                // Verificar requisitos mínimos de contraseña
                if (viewModel.Password.Length < 6)
                {
                    _logger.LogWarning("Contraseña demasiado corta ({Length}) para usuario {Id}", 
                        viewModel.Password.Length, viewModel.UsuarioId);
                    this.Error("La contraseña debe tener al menos 6 caracteres");
                    return View(viewModel);
                }
                
                // Verificar que el usuario exista antes de intentar cambiar la contraseña
                var usuario = await _usuarioService.GetByIdAsync(viewModel.UsuarioId);
                if (usuario == null)
                {
                    _logger.LogWarning("Intento de cambiar contraseña para usuario inexistente: {Id}", viewModel.UsuarioId);
                    this.Error("Usuario no encontrado");
                    return RedirectToAction(nameof(Index));
                }
                
                // Antes de la llamada - loguear información
                _logger.LogInformation("Llamando a CambiarPasswordAsync para usuario {Id}, longitud de contraseña: {Length}", 
                    viewModel.UsuarioId, viewModel.Password.Length);
                    
                var result = await _usuarioService.CambiarPasswordAsync(viewModel.UsuarioId, viewModel.Password);
                
                // Después de la llamada - loguear resultado
                _logger.LogInformation("Resultado de CambiarPasswordAsync para usuario {Id}: Success={Success}, Message={Message}", 
                    viewModel.UsuarioId, result.Success, result.Message);
                
                if (result.Success)
                {
                    _logger.LogInformation("Contraseña cambiada exitosamente para usuario {Id}", viewModel.UsuarioId);
                    this.Success("Contraseña cambiada correctamente");
                    return RedirectToAction(nameof(Index));
                }
                
                _logger.LogWarning("Error al cambiar contraseña (desde el servicio): {Error}", result.ErrorMessage);
                this.Error(string.IsNullOrEmpty(result.ErrorMessage) 
                    ? "Error desconocido al cambiar la contraseña" 
                    : $"Error al cambiar la contraseña: {result.ErrorMessage}");
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
