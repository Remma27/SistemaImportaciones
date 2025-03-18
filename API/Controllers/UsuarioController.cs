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

        public UsuarioController(ApiContext context, PasswordHashService passwordHashService, HistorialService historialService)
        {
            _context = context;
            _passwordHashService = passwordHashService;
            _historialService = historialService;
        }

        // Endpoint para crear un nuevo Usuario
        [HttpPost]
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
                
                _context.Usuarios.Add(usuario);
                _context.SaveChanges();
                
                // Crear una copia para el historial sin el password
                var usuarioParaHistorial = new { 
                    Id = usuario.id,
                    Nombre = usuario.nombre,
                    Email = usuario.email,
                    Activo = usuario.activo
                };
                
                // Registrar en historial
                _historialService.GuardarHistorial("CREAR", usuarioParaHistorial, "Usuarios", $"Creación de usuario: {usuario.email}");
            }
            else
            {
                return new JsonResult(BadRequest("La contraseña no puede estar vacía."));
            }
            
            return new JsonResult(Ok(usuario));
        }

        // Endpoint para editar un Usuario existente
        [HttpPut]
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

        // Get
        [HttpGet]
        public JsonResult Get(int id)
        {
            var result = _context.Usuarios.Find(id);
            if (result == null)
            {
                return new JsonResult(NotFound());
            }
            
            // No enviar el hash de la contraseña
            result.password_hash = null;
            
            return new JsonResult(Ok(result));
        }

        // Delete
        [HttpDelete]
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

        // GetAll
        [HttpGet]
        public JsonResult GetAll()
        {
            var result = _context.Usuarios.ToList();
            
            // No enviar los hashes de contraseñas
            foreach (var usuario in result)
            {
                usuario.password_hash = null;
            }
            
            return new JsonResult(Ok(result));
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
                
                _context.Usuarios.Add(model);
                _context.SaveChanges();
                
                // Crear una copia para el historial sin el password
                var usuarioParaHistorial = new { 
                    Id = model.id,
                    Nombre = model.nombre,
                    Email = model.email,
                    Activo = model.activo
                };
                
                // Registrar en historial con usuario del sistema (ya que estamos en un endpoint no autenticado)
                _historialService.GuardarHistorial("CREAR", usuarioParaHistorial, "Usuarios", $"Creación de usuario: {model.email}");
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
                    .FirstOrDefaultAsync(u => (u.email ?? string.Empty).ToLower() == userEmail.ToLower());

                if (usuario == null || usuario.password_hash == null ||
                    !_passwordHashService.VerifyPassword(model.Password, usuario.password_hash))
                {
                    return BadRequest(new { message = "Credenciales inválidas" });
                }

                usuario.ultimo_acceso = DateTime.Now;
                await _context.SaveChangesAsync();

                // Create claims for authentication
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, usuario.nombre ?? string.Empty),
                    new Claim(ClaimTypes.Email, usuario.email ?? string.Empty),
                    new Claim(ClaimTypes.NameIdentifier, usuario.id.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                };

                // Sign the user in with cookie authentication
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Create a sanitized user object that excludes sensitive information
                var userResponse = new
                {
                    id = usuario.id,
                    email = usuario.email,
                    nombre = usuario.nombre,
                    ultimo_acceso = usuario.ultimo_acceso,
                    activo = usuario.activo
                };
                
                // Registrar historial de login
                _historialService.GuardarHistorial("LOGIN", new { UserId = usuario.id, Email = usuario.email, Time = DateTime.Now }, 
                    "Autenticación", $"Inicio de sesión: {usuario.email}");

                // Return the sanitized user object
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
    }
}
