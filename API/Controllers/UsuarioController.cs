using Microsoft.AspNetCore.Mvc;
using API.Models;
using API.Data;
using Sistema_de_Gestion_de_Importaciones.ViewModels;
using API.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

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

        // Endpoint para crear un nuevo Usuario
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public JsonResult Create(Usuario usuario)
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
                // Guarda una copia del password original para no guardarlo en historial
                string passwordOriginal = usuario.password_hash;
                usuario.password_hash = _passwordHashService.HashPassword(usuario.password_hash);
                
                // Verificar si se proporcionó un rol_id, si no, asignar rol por defecto (Operador)
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
                
                // Obtener el nombre del rol para el historial
                string rolNombre = "Sin rol";
                if (usuario.rol_id.HasValue)
                {
                    var rol = _context.Roles.Find(usuario.rol_id.Value);
                    if (rol != null)
                    {
                        rolNombre = rol.nombre;
                    }
                }
                
                // Crear una copia para el historial sin el password
                var usuarioParaHistorial = new { 
                    Id = usuario.id,
                    Nombre = usuario.nombre,
                    Email = usuario.email,
                    Activo = usuario.activo,
                    Rol = rolNombre
                };
                
                // Registrar en historial
                _historialService.GuardarHistorial("CREAR", usuarioParaHistorial, "Usuarios", $"Creación de usuario: {usuario.email} con rol {rolNombre}");
            }
            else
            {
                return new JsonResult(BadRequest("La contraseña no puede estar vacía."));
            }
            
            return new JsonResult(Ok(usuario));
        }

        // Endpoint para editar un Usuario existente
        [HttpPut]
        [Authorize(Roles = "Administrador")]
        public JsonResult Edit(Usuario usuario)
        {
            if (usuario.id == 0)
            {
                return new JsonResult(BadRequest("Debe proporcionar un id válido para editar un usuario."));
            }
            
            var usuarioInDb = _context.Usuarios.Find(usuario.id);
            if (usuarioInDb == null)
            {
                return new JsonResult(NotFound());
            }

            // Crear una copia para el historial sin el password
            var usuarioAnteriorParaHistorial = new Usuario
            {
                id = usuarioInDb.id,
                nombre = usuarioInDb.nombre,
                email = usuarioInDb.email,
                password_hash = "[PROTEGIDO]",
                fecha_creacion = usuarioInDb.fecha_creacion,
                ultimo_acceso = usuarioInDb.ultimo_acceso,
                activo = usuarioInDb.activo
            };
            
            // Registrar estado anterior claramente
            _historialService.GuardarHistorial(
                "ANTES_EDITAR", 
                usuarioAnteriorParaHistorial, 
                "Usuarios", 
                $"Estado anterior de usuario {usuarioInDb.email} (ID: {usuarioInDb.id})"
            );
            
            // Si se proporciona un nueva contraseña, hasheala
            if (!string.IsNullOrEmpty(usuario.password_hash) && !usuario.password_hash.StartsWith("$2a$"))
            {
                usuario.password_hash = _passwordHashService.HashPassword(usuario.password_hash);
            }
            else
            {
                // Mantener la contraseña existente si no se proporciona una nueva
                usuario.password_hash = usuarioInDb.password_hash;
            }

            _context.Entry(usuarioInDb).CurrentValues.SetValues(usuario);
            _context.SaveChanges();
            
            // Crear una copia para el historial sin el password
            var usuarioNuevoParaHistorial = new Usuario
            {
                id = usuario.id,
                nombre = usuario.nombre,
                email = usuario.email,
                password_hash = "[PROTEGIDO]",
                fecha_creacion = usuario.fecha_creacion,
                ultimo_acceso = usuario.ultimo_acceso,
                activo = usuario.activo
            };
            
            // Registrar estado nuevo claramente
            _historialService.GuardarHistorial(
                "DESPUES_EDITAR", 
                usuarioNuevoParaHistorial, 
                "Usuarios", 
                $"Estado nuevo de usuario {usuario.email} (ID: {usuario.id})"
            );
            
            return new JsonResult(Ok(usuario));
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
                
                // Crear un DTO simplificado para evitar ciclos de referencia
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
            
            // Crear una copia para el historial sin el password
            var usuarioParaHistorial = new { 
                Id = result.id,
                Nombre = result.nombre,
                Email = result.email,
                Activo = result.activo
            };
            
            // Registrar antes de eliminar
            _historialService.GuardarHistorial("ELIMINAR", usuarioParaHistorial, "Usuarios", $"Eliminación de usuario ID: {id}");
            
            _context.Usuarios.Remove(result);
            _context.SaveChanges();
            return new JsonResult(NoContent());
        }

        [HttpGet]
        public JsonResult GetAll()
        {
            try 
            {
                // Obtener los usuarios con sus roles
                var usuarios = _context.Usuarios.Include(u => u.Rol).ToList();
                
                // Crear un DTO simplificado para evitar ciclos de referencia
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
                
                return new JsonResult(Ok(result));
            }
            catch (Exception ex)
            {
                return new JsonResult(StatusCode(500, new { message = "Error obteniendo usuarios", error = ex.Message }));
            }
        }

        // Registrar
        [HttpPost]
        [AllowAnonymous]
        public JsonResult Registrar(Usuario model)
        {
            var usuario = _context.Usuarios.FirstOrDefault(u => u.email == model.email);
            if (usuario != null)
            {
                return new JsonResult(BadRequest("El email ya está registrado."));
            }

            if (model.password_hash != null)
            {
                // Guarda una copia del password original para no guardarlo en historial
                string passwordOriginal = model.password_hash;
                model.password_hash = _passwordHashService.HashPassword(model.password_hash);
                
                // Asignar rol por defecto (Operador) para usuarios registrados
                var rolOperador = _context.Roles.FirstOrDefault(r => r.nombre == "Operador");
                if (rolOperador != null)
                {
                    model.rol_id = rolOperador.id;
                }
                
                _context.Usuarios.Add(model);
                _context.SaveChanges();
                
                // Obtener el nombre del rol para el historial
                string rolNombre = "Sin rol";
                if (model.rol_id.HasValue)
                {
                    var rol = _context.Roles.Find(model.rol_id.Value);
                    if (rol != null)
                    {
                        rolNombre = rol.nombre;
                    }
                }
                
                // Crear una copia para el historial sin el password
                var usuarioParaHistorial = new { 
                    Id = model.id,
                    Nombre = model.nombre,
                    Email = model.email,
                    Activo = model.activo,
                    Rol = rolNombre
                };
                
                // Registrar en historial con usuario del sistema (ya que estamos en un endpoint no autenticado)
                _historialService.GuardarHistorial("CREAR", usuarioParaHistorial, "Usuarios", $"Creación de usuario: {model.email} con rol {rolNombre}");
            }
            else
            {
                return new JsonResult(BadRequest("La contraseña no puede estar vacía."));
            }
            
            return new JsonResult(Ok(model));
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<Usuario>> IniciarSesion([FromBody] LoginViewModel model)
        {
            try
            {
                string userEmail = model.Email ?? string.Empty;
                var usuario = await _context.Usuarios
                    .Include(u => u.Rol) // Incluir el rol del usuario
                    .FirstOrDefaultAsync(u => (u.email ?? string.Empty).ToLower() == userEmail.ToLower());

                if (usuario == null || usuario.password_hash == null ||
                    !_passwordHashService.VerifyPassword(model.Password, usuario.password_hash))
                {
                    return BadRequest(new { message = "Credenciales inválidas" });
                }

                usuario.ultimo_acceso = DateTime.Now;
                await _context.SaveChangesAsync();

                // Determinar el rol del usuario de forma explícita
                string rolNombre = "Usuario"; // Valor por defecto
                bool esAdmin = false;

                if (usuario.rol_id.HasValue)
                {
                    // Obtener el rol directamente de la base de datos
                    var rol = await _context.Roles.FindAsync(usuario.rol_id.Value);
                    if (rol != null)
                    {
                        rolNombre = rol.nombre;
                        esAdmin = rolNombre.Equals("Administrador", StringComparison.OrdinalIgnoreCase);
                    }
                }

                // Lista para almacenar los claims con múltiples formatos de claim de rol para máxima compatibilidad
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, usuario.nombre ?? string.Empty),
                    new Claim(ClaimTypes.Email, usuario.email ?? string.Empty),
                    new Claim(ClaimTypes.NameIdentifier, usuario.id.ToString()),
                    
                    // Agregar el claim de rol en TODOS los formatos posibles para maximizar compatibilidad
                    new Claim(ClaimTypes.Role, rolNombre),
                    new Claim("role", rolNombre),
                    new Claim("Role", rolNombre),
                    new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", rolNombre)
                };
                
                // Si es admin, asegurar que tenga ese rol explícitamente
                if (esAdmin)
                {
                    claims.Add(new Claim("IsAdmin", "true"));
                }
                
                // Agregar permisos según el rol
                if (usuario.rol_id.HasValue)
                {
                    var permisos = await _rolPermisoService.ObtenerPermisosPorRolId(usuario.rol_id.Value);
                    foreach (var permiso in permisos)
                    {
                        claims.Add(new Claim("Permission", permiso.nombre));
                    }
                    
                    // Si es administrador, añadir todos los permisos posibles
                    if (esAdmin)
                    {
                        var todosLosPermisos = await _context.Permisos.ToListAsync();
                        foreach (var permiso in todosLosPermisos)
                        {
                            // Evitar duplicados
                            if (!claims.Any(c => c.Type == "Permission" && c.Value == permiso.nombre))
                            {
                                claims.Add(new Claim("Permission", permiso.nombre));
                            }
                        }
                    }
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                
                // Crear un nuevo ClaimsPrincipal para verificar que IsInRole funcione
                var principal = new ClaimsPrincipal(claimsIdentity);
                bool rolFuncionando = principal.IsInRole(rolNombre);
                
                // Si el rol no funciona, intentar añadir más claims de forma diferente
                if (!rolFuncionando)
                {
                    // Crear una nueva identidad con el claim de rol explícito
                    var roleIdentity = new ClaimsIdentity();
                    roleIdentity.AddClaim(new Claim(ClaimTypes.Role, rolNombre));
                    principal.AddIdentity(roleIdentity);
                }

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
                    // Añadir indicadores adicionales para el rol
                    Items = { {"role", rolNombre} }
                };

                // Firmar la autenticación con el principal actualizado
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    authProperties);

                // Verificar después de la autenticación si el rol se está reconociendo
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
                    new { UserId = usuario.id, Email = usuario.email, Time = DateTime.Now, Rol = rolNombre }, 
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
            // Obtener ID de usuario para el historial antes de cerrar sesión
            int userId = 0;
            string userEmail = "desconocido";
            
            if (HttpContext.User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
                var emailClaim = HttpContext.User.FindFirst(ClaimTypes.Email);
                
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out userId))
                {
                    // ID obtenido correctamente
                }
                
                if (emailClaim != null)
                {
                    userEmail = emailClaim.Value;
                }
                
                // Registrar historial de logout
                _historialService.GuardarHistorial("LOGOUT", new { UserId = userId, Email = userEmail, Time = DateTime.Now }, 
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

        // Nuevo endpoint para asignar rol a usuario
        [HttpPost("AsignarRol")]
        [Authorize(Roles = "Administrador")] // Solo administradores pueden asignar roles
        public async Task<IActionResult> AsignarRol([FromBody] AsignarRolViewModel model)
        {
            try
            {
                var usuario = await _context.Usuarios.FindAsync(model.UsuarioId);
                if (usuario == null)
                    return NotFound(new { message = "Usuario no encontrado" });

                var rol = await _context.Roles.FindAsync(model.RolId);
                if (rol == null)
                    return NotFound(new { message = "Rol no encontrado" });

                usuario.rol_id = rol.id;
                await _context.SaveChangesAsync();

                _historialService.GuardarHistorial(
                    "ASIGNAR_ROL",
                    new { UsuarioId = usuario.id, RolId = rol.id, RolNombre = rol.nombre },
                    "Usuarios",
                    $"Asignación de rol '{rol.nombre}' al usuario '{usuario.email}'"
                );

                return Ok(new { message = "Rol asignado con éxito", usuario = new { id = usuario.id, nombre = usuario.nombre, email = usuario.email, rol = rol.nombre } });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
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
                
                // Solo cambiamos el estado activo, no tocamos la contraseña
                usuarioInDb.activo = activo;
                
                _context.SaveChanges();
                
                // Registrar en historial
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
    }
}
