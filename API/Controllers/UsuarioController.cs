﻿using Microsoft.AspNetCore.Mvc;
using API.Models;
using API.Data;
using Sistema_de_Gestion_de_Importaciones.ViewModels;
using API.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class UsuarioController : ControllerBase
    {
        private readonly ApiContext _context;
        private readonly PasswordHashService _passwordHashService;
        private readonly HistorialService _historialService;
        private readonly RolPermisoService _rolPermisoService;

        public UsuarioController(ApiContext context, PasswordHashService passwordHashService, HistorialService historialService, RolPermisoService rolPermisoService)
        {
            _context = context;
            _passwordHashService = passwordHashService;
            _historialService = historialService;
            _rolPermisoService = rolPermisoService;
        }

        private DateTime GetCostaRicaTime()
        {
            try
            {
                var costaRicaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, costaRicaTimeZone);
            }
            catch (Exception)
            {
                return DateTime.UtcNow.AddHours(-6);
            }
        }

        // Endpoint para crear un nuevo Usuario
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public JsonResult Create(Usuario usuario)
        {
            try
            {
                if (usuario.id != 0)
                {
                    return new JsonResult(BadRequest("El id debe ser 0 para crear un nuevo usuario."));
                }

                var usuarioExistente = _context.Usuarios.FirstOrDefault(u => u.email == usuario.email);
                if (usuarioExistente != null)
                {
                    return new JsonResult(BadRequest("El email ya está registrado."));
                }

                if (usuario.password_hash != null)
                {
                    string passwordOriginal = usuario.password_hash;
                    usuario.password_hash = _passwordHashService.HashPassword(usuario.password_hash);
                    
                    if (!usuario.rol_id.HasValue)
                    {
                        var rolOperador = _context.Roles.FirstOrDefault(r => r.nombre == "Operador");
                        if (rolOperador != null)
                        {
                            usuario.rol_id = rolOperador.id;
                        }
                    }
                    
                    _context.Usuarios.Add(usuario);
                    _context.SaveChanges();
                    
                    var jsonOptions = new JsonSerializerOptions
                    {
                        ReferenceHandler = ReferenceHandler.Preserve,
                        MaxDepth = 64,
                        WriteIndented = true
                    };
                    
                    string rolNombre = "Sin rol";
                    if (usuario.rol_id.HasValue)
                    {
                        var rol = _context.Roles.Find(usuario.rol_id.Value);
                        if (rol != null)
                        {
                            rolNombre = rol.nombre;
                        }
                    }
                    
                    var usuarioParaHistorial = new { 
                        Id = usuario.id,
                        Nombre = usuario.nombre,
                        Email = usuario.email,
                        Activo = usuario.activo,
                        Rol = rolNombre
                    };
                    
                    _historialService.GuardarHistorial("CREAR", usuarioParaHistorial, "Usuarios", $"Creación de usuario: {usuario.email} con rol {rolNombre}");
                    
                    var usuarioResponse = new {
                        usuario.id,
                        usuario.nombre,
                        usuario.email,
                        usuario.activo,
                        usuario.fecha_creacion,
                        rol = new { id = usuario.rol_id, nombre = rolNombre }
                    };
                    
                    return new JsonResult(Ok(usuarioResponse));
                }
                else
                {
                    return new JsonResult(BadRequest("La contraseña no puede estar vacía."));
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(StatusCode(500, new { message = $"Error al crear el usuario: {ex.Message}" }));
            }
        }

        [HttpPut]
        [Authorize(Roles = "Administrador")]
        public JsonResult Edit(Usuario usuario)
        {
            try 
            {
                if (usuario.id == 0)
                {
                    return new JsonResult(BadRequest("Debe proporcionar un id válido para editar un usuario."));
                }
                
                var usuarioInDb = _context.Usuarios.Find(usuario.id);
                if (usuarioInDb == null)
                {
                    return new JsonResult(NotFound(new { message = $"Usuario con ID {usuario.id} no encontrado" }));
                }

                _historialService.GuardarHistorial(
                    "ANTES_EDITAR", 
                    new { 
                        Id = usuarioInDb.id,
                        Nombre = usuarioInDb.nombre,
                        Email = usuarioInDb.email,
                        Activo = usuarioInDb.activo,
                        RolId = usuarioInDb.rol_id
                    }, 
                    "Usuarios", 
                    $"Estado anterior de usuario {usuarioInDb.email} (ID: {usuarioInDb.id})"
                );
                

                usuarioInDb.nombre = usuario.nombre;
                usuarioInDb.email = usuario.email;
                usuarioInDb.activo = usuario.activo;
                
                if (usuario.rol_id.HasValue)
                {
                    usuarioInDb.rol_id = usuario.rol_id;
                }
                
                _context.SaveChanges();
                
                _historialService.GuardarHistorial(
                    "DESPUES_EDITAR", 
                    new { 
                        Id = usuarioInDb.id,
                        Nombre = usuarioInDb.nombre,
                        Email = usuarioInDb.email,
                        Activo = usuarioInDb.activo,
                        RolId = usuarioInDb.rol_id
                    }, 
                    "Usuarios", 
                    $"Estado nuevo de usuario {usuarioInDb.email} (ID: {usuarioInDb.id})"
                );
                
                var usuarioResponse = new {
                    usuarioInDb.id,
                    usuarioInDb.nombre,
                    usuarioInDb.email,
                    usuarioInDb.activo,
                    usuarioInDb.fecha_creacion,
                    usuarioInDb.ultimo_acceso,
                    rol_id = usuarioInDb.rol_id
                };
                
                return new JsonResult(Ok(usuarioResponse));
            }
            catch (Exception ex)
            {
                return new JsonResult(StatusCode(500, new { message = $"Error al actualizar el usuario: {ex.Message}" }));
            }
        }

        [HttpGet]
        public JsonResult Get(int id)
        {
            try
            {
                var usuario = _context.Usuarios.Include(u => u.Rol).FirstOrDefault(u => u.id == id);
                if (usuario == null)
                {
                    return new JsonResult(NotFound(new { message = $"Usuario con ID {id} no encontrado" }));
                }
                
                var result = new
                {
                    usuario.id,
                    usuario.nombre,
                    usuario.email,
                    usuario.activo,
                    usuario.fecha_creacion,
                    usuario.ultimo_acceso,
                    usuario.rol_id,
                    Rol = usuario.Rol != null ? new { usuario.Rol.id, usuario.Rol.nombre, usuario.Rol.descripcion } : null
                };
                
                return new JsonResult(Ok(result));
            }
            catch (Exception ex)
            {
                return new JsonResult(StatusCode(500, new { message = $"Error obteniendo usuario con ID {id}", error = ex.Message }));
            }
        }

        // Delete
        [HttpDelete]
        [Authorize(Roles = "Administrador")]
        public JsonResult Delete(int id)
        {
            var result = _context.Usuarios.Find(id);
            if (result == null)
            {
                return new JsonResult(NotFound());
            }
            
            var usuarioParaHistorial = new { 
                Id = result.id,
                Nombre = result.nombre,
                Email = result.email,
                Activo = result.activo
            };
            
            _historialService.GuardarHistorial("ELIMINAR", usuarioParaHistorial, "Usuarios", $"Eliminación de usuario ID: {id}");
            
            _context.Usuarios.Remove(result);
            _context.SaveChanges();
            return new JsonResult(NoContent());
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try 
            {
                var usuarios = await _context.Usuarios.Include(u => u.Rol).ToListAsync();
                
                var result = usuarios.Select(u => new
                {
                    u.id,
                    u.nombre,
                    u.email,
                    u.activo,
                    u.fecha_creacion,
                    u.ultimo_acceso,
                    u.rol_id,
                    Rol = u.Rol != null ? new { u.Rol.id, u.Rol.nombre, u.Rol.descripcion } : null
                }).ToList();
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error obteniendo usuarios", error = ex.Message });
            }
        }

        // Registrar
        [HttpPost]
        [AllowAnonymous]
        public JsonResult Registrar(Usuario model)
        {
            try 
            {
                var usuario = _context.Usuarios.FirstOrDefault(u => u.email == model.email);
                if (usuario != null)
                {
                    return new JsonResult(BadRequest("El email ya está registrado."));
                }

                if (model.password_hash != null)
                {
                    string passwordOriginal = model.password_hash;
                    model.password_hash = _passwordHashService.HashPassword(model.password_hash);
                    
                    var rolOperador = _context.Roles.FirstOrDefault(r => r.nombre == "Operador");
                    if (rolOperador != null)
                    {
                        model.rol_id = rolOperador.id;
                    }
                    
                    _context.Usuarios.Add(model);
                    _context.SaveChanges();
                    
                    string rolNombre = "Sin rol";
                    if (model.rol_id.HasValue)
                    {
                        var rol = _context.Roles.Find(model.rol_id.Value);
                        if (rol != null)
                        {
                            rolNombre = rol.nombre;
                        }
                    }
                    
                    var usuarioParaHistorial = new { 
                        Id = model.id,
                        Nombre = model.nombre,
                        Email = model.email,
                        Activo = model.activo,
                        Rol = rolNombre
                    };
                    
                    _historialService.GuardarHistorial("CREAR", usuarioParaHistorial, "Usuarios", $"Creación de usuario: {model.email} con rol {rolNombre}");
                    
                    var usuarioResponse = new {
                        model.id,
                        model.nombre,
                        model.email,
                        model.activo,
                        model.fecha_creacion,
                        rol = new { id = model.rol_id, nombre = rolNombre }
                    };
                    
                    return new JsonResult(Ok(usuarioResponse));
                }
                else
                {
                    return new JsonResult(BadRequest("La contraseña no puede estar vacía."));
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(StatusCode(500, new { message = $"Error al registrar el usuario: {ex.Message}" }));
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<Usuario>> IniciarSesion([FromBody] LoginViewModel model)
        {
            try
            {
                string userEmail = model.Email ?? string.Empty;
                var usuario = await _context.Usuarios
                    .Include(u => u.Rol) 
                    .FirstOrDefaultAsync(u => (u.email ?? string.Empty).ToLower() == userEmail.ToLower());

                if (usuario == null || usuario.password_hash == null ||
                    !_passwordHashService.VerifyPassword(model.Password, usuario.password_hash))
                {
                    return BadRequest(new { message = "Credenciales inválidas" });
                }

                if (usuario.activo != true)
                {
                    _historialService.GuardarHistorial(
                        "LOGIN_FALLIDO", 
                        new { UserId = usuario.id, Email = usuario.email, Time = GetCostaRicaTime(), Motivo = "Usuario inactivo" }, 
                        "Autenticación", 
                        $"Intento de inicio de sesión rechazado: {usuario.email} (Usuario inactivo)"
                    );
                    return BadRequest(new { message = "Su cuenta está desactivada. Contacte al administrador." });
                }

                usuario.ultimo_acceso = GetCostaRicaTime();
                await _context.SaveChangesAsync();

                string rolNombre = "Usuario"; 
                bool esAdmin = false;

                if (usuario.rol_id.HasValue)
                {
                    var rol = await _context.Roles.FindAsync(usuario.rol_id.Value);
                    if (rol != null)
                    {
                        rolNombre = rol.nombre;
                        esAdmin = rolNombre.Equals("Administrador", StringComparison.OrdinalIgnoreCase);
                    }
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, usuario.nombre ?? string.Empty),
                    new Claim(ClaimTypes.Email, usuario.email ?? string.Empty),
                    new Claim(ClaimTypes.NameIdentifier, usuario.id.ToString()),
                    
                    new Claim(ClaimTypes.Role, rolNombre),
                    new Claim("role", rolNombre),
                    new Claim("Role", rolNombre),
                    new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", rolNombre)
                };
                
                if (esAdmin)
                {
                    claims.Add(new Claim("IsAdmin", "true"));
                }
                
                if (usuario.rol_id.HasValue)
                {
                    try
                    {
                        var permisos = await _rolPermisoService.ObtenerPermisosPorRolId(usuario.rol_id.Value);
                        foreach (var permiso in permisos)
                        {
                            claims.Add(new Claim("Permission", permiso.nombre));
                        }
                        
                        if (esAdmin)
                        {
                            var todosLosPermisos = await _context.Permisos.ToListAsync();
                            foreach (var permiso in todosLosPermisos)
                            {
                                if (!claims.Any(c => c.Type == "Permission" && c.Value == permiso.nombre))
                                {
                                    claims.Add(new Claim("Permission", permiso.nombre));
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                    if (esAdmin)
                        {
                            claims.Add(new Claim("AllPermissions", "true"));
                        }
                    }
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                
                var principal = new ClaimsPrincipal(claimsIdentity);
                bool rolFuncionando = principal.IsInRole(rolNombre);
                
                if (!rolFuncionando)
                {
                    var roleIdentity = new ClaimsIdentity();
                    roleIdentity.AddClaim(new Claim(ClaimTypes.Role, rolNombre));
                    principal.AddIdentity(roleIdentity);
                }

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
                    Items = { {"role", rolNombre} }
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    authProperties);

                var currentUser = HttpContext.User;
                bool adminReconocidoAhora = currentUser.IsInRole("Administrador");

                var userResponse = new
                {
                    id = usuario.id,
                    email = usuario.email,
                    nombre = usuario.nombre,
                    ultimo_acceso = usuario.ultimo_acceso,
                    activo = usuario.activo,
                    rol = rolNombre,
                    esAdmin = esAdmin,
                    rolReconocido = rolFuncionando,
                    claims = claims.Select(c => new { c.Type, c.Value }).ToList()
                };
                
                _historialService.GuardarHistorial("LOGIN", 
                    new { UserId = usuario.id, Email = usuario.email, Time = GetCostaRicaTime(), Rol = rolNombre }, 
                    "Autenticación", 
                    $"Inicio de sesión: {usuario.email} con rol {rolNombre}");

                return Ok(new
                {
                    Message = "Inicio de sesión exitoso",
                    User = userResponse
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // Add a logout endpoint
        [HttpPost]
        public async Task<IActionResult> CerrarSesion()
        {
            int userId = 0;
            string userEmail = "desconocido";
            
            if (HttpContext.User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
                var emailClaim = HttpContext.User.FindFirst(ClaimTypes.Email);
                
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out userId))
                {
                    // Obtener el ID del usuario
                }
                
                if (emailClaim != null)
                {
                    userEmail = emailClaim.Value;
                }
                
                _historialService.GuardarHistorial("LOGOUT", new { UserId = userId, Email = userEmail, Time = GetCostaRicaTime() }, 
                    "Autenticación", $"Cierre de sesión: {userEmail}");
            }
            
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie, new CookieOptions
                {
                    Secure = true,
                    HttpOnly = true,
                    SameSite = SameSiteMode.Strict
                });
            }

            Response.Cookies.Delete(CookieAuthenticationDefaults.AuthenticationScheme, new CookieOptions
            {
                Secure = true,
                HttpOnly = true,
                SameSite = SameSiteMode.Strict
            });

            return Ok(new
            {
                Message = "Sesión cerrada correctamente",
                CookiesRemoved = Request.Cookies.Keys.ToList()
            });
        }

        // Endpoint para obtener todos los roles
        [HttpGet("Roles")]
        [Authorize]
        public async Task<IActionResult> ObtenerRoles()
        {
            try
            {
                var roles = await _rolPermisoService.ObtenerTodosLosRoles();
                return Ok(roles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener roles", error = ex.Message });
            }
        }

        // Endpoint para verificar los permisos del usuario actual (opcional, puede eliminarse en producción)
        [HttpGet("VerificarPermisos")]
        public IActionResult VerificarPermisos()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            var esAdmin = User.IsInRole("Administrador");
            
            return Ok(new
            {
                esAutenticado = User.Identity?.IsAuthenticated ?? false,
                nombreUsuario = User.Identity?.Name,
                roles,
                esAdmin,
                claims
            });
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public JsonResult ToggleActivo(int id, bool activo)
        {
            try
            {
                var usuarioInDb = _context.Usuarios.Find(id);
                if (usuarioInDb == null)
                {
                    return new JsonResult(NotFound(new { message = $"Usuario con ID {id} no encontrado" }));
                }
                
                usuarioInDb.activo = activo;
                
                _context.SaveChanges();
                
                _historialService.GuardarHistorial(
                    activo ? "ACTIVAR_USUARIO" : "DESACTIVAR_USUARIO",
                    new { Id = usuarioInDb.id, Nombre = usuarioInDb.nombre, Email = usuarioInDb.email },
                    "Usuarios",
                    $"Usuario {usuarioInDb.email} {(activo ? "activado" : "desactivado")} por un administrador"
                );
                
                return new JsonResult(Ok(new { message = $"Usuario {(activo ? "activado" : "desactivado")} correctamente" }));
            }
            catch (Exception ex)
            {
                return new JsonResult(StatusCode(500, new { message = "Error al cambiar estado del usuario", error = ex.Message }));
            }
        }

        // Nuevo endpoint para asignar rol a usuario
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public JsonResult AsignarRol([FromBody] AsignarRolViewModel model)
        {
            try
            {
                if (model == null)
                    return new JsonResult(BadRequest("Datos inválidos"));
                    
                Console.WriteLine($"[DEBUG-API] Recibida solicitud AsignarRol - UsuarioId: {model.UsuarioId}, RolId: {model.RolId}");
                    
                var usuario = _context.Usuarios.Find(model.UsuarioId);
                if (usuario == null)
                {
                    Console.WriteLine($"[DEBUG-API] Usuario no encontrado: {model.UsuarioId}");
                    return new JsonResult(NotFound(new { message = "Usuario no encontrado" }));
                }

                var rol = _context.Roles.Find(model.RolId);
                if (rol == null)
                {
                    Console.WriteLine($"[DEBUG-API] Rol no encontrado: {model.RolId}");
                    return new JsonResult(NotFound(new { message = "Rol no encontrado" }));
                }

                usuario.rol_id = rol.id;
                _context.SaveChanges();
                
                Console.WriteLine($"[DEBUG-API] Rol asignado correctamente - Usuario: {usuario.email}, Rol: {rol.nombre}");

                _historialService.GuardarHistorial(
                    "ASIGNAR_ROL",
                    new { UsuarioId = usuario.id, RolId = rol.id, RolNombre = rol.nombre },
                    "Usuarios",
                    $"Asignación de rol '{rol.nombre}' al usuario '{usuario.email}'"
                );

                return new JsonResult(Ok(new { message = "Rol asignado con éxito", usuario = new { id = usuario.id, nombre = usuario.nombre, email = usuario.email, rol = rol.nombre } }));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG-API] Error en AsignarRol: {ex.Message}");
                return new JsonResult(StatusCode(500, new { message = "Error interno del servidor", error = ex.Message }));
            }
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public IActionResult CambiarPassword([FromBody] CambiarPasswordRequest model)
        {
            try
            {
                Console.WriteLine($"[DEBUG] Recibida petición de cambio de contraseña para usuario ID: {model.UsuarioId}");
                
                if (model == null || model.UsuarioId <= 0)
                {
                    Console.WriteLine("[ERROR] Datos de modelo inválidos o ID de usuario <= 0");
                    return BadRequest(new { message = "Datos inválidos para cambiar contraseña" });
                }

                var usuario = _context.Usuarios.Find(model.UsuarioId);
                if (usuario == null)
                {
                    Console.WriteLine($"[ERROR] No se encontró el usuario con ID: {model.UsuarioId}");
                    return NotFound(new { message = $"No se encontró el usuario con ID {model.UsuarioId}" });
                }

                if (string.IsNullOrEmpty(model.NewPassword))
                {
                    Console.WriteLine("[ERROR] La nueva contraseña está vacía");
                    return BadRequest(new { message = "La nueva contraseña no puede estar vacía" });
                }

                Console.WriteLine($"[DEBUG] Creando hash para nueva contraseña del usuario {usuario.email}");
                
                string hashedPassword;
                try
                {
                    hashedPassword = _passwordHashService.HashPassword(model.NewPassword);
                }
                catch (Exception hashEx)
                {
                    Console.WriteLine($"[ERROR] Error al generar hash de contraseña: {hashEx.Message}");
                    return StatusCode(500, new { message = $"Error al procesar la contraseña: {hashEx.Message}" });
                }

                _historialService.GuardarHistorial(
                    "ANTES_CAMBIAR_PASSWORD",
                    new { Id = usuario.id, Nombre = usuario.nombre, Email = usuario.email },
                    "Usuarios",
                    $"Cambio de contraseña para usuario {usuario.email} (ID: {usuario.id})"
                );

                usuario.password_hash = hashedPassword;
                _context.SaveChanges();

                Console.WriteLine($"[DEBUG] Contraseña actualizada exitosamente para usuario {usuario.email}");

                _historialService.GuardarHistorial(
                    "CAMBIAR_PASSWORD",
                    new { Id = usuario.id, Nombre = usuario.nombre, Email = usuario.email },
                    "Usuarios",
                    $"Contraseña cambiada para usuario {usuario.email} (ID: {usuario.id})"
                );

                return Ok(new { message = "Contraseña actualizada correctamente" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error al cambiar contraseña: {ex.Message}");
                Console.WriteLine($"[ERROR] StackTrace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[ERROR] Inner Exception: {ex.InnerException.Message}");
                }
                
                return StatusCode(500, new { message = $"Error al cambiar la contraseña: {ex.Message}" });
            }
        }

        public class CambiarPasswordRequest
        {
            public int UsuarioId { get; set; }
            public string? NewPassword { get; set; }
        }
    }
}
