using Microsoft.AspNetCore.Mvc;
using API.Models;
using API.Data;
using Sistema_de_Gestion_de_Importaciones.ViewModels;
using API.Services;

namespace API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class UsuarioController : ControllerBase
    {
        private readonly ApiContext _context;
        private readonly PasswordHashService _passwordHashService;

        public UsuarioController(ApiContext context, PasswordHashService passwordHashService)
        {
            _context = context;
            _passwordHashService = passwordHashService;
        }

        // Endpoint para crear un nuevo Usuario
        [HttpPost]
        public JsonResult Create(Usuario usuario)
        {
            if (usuario.id != 0)
            {
                return new JsonResult(BadRequest("El id debe ser 0 para crear un nuevo usuario."));
            }

            // Verificar si el email ya existe
            var usuarioExistente = _context.Usuarios.FirstOrDefault(u => u.email == usuario.email);
            if (usuarioExistente != null)
            {
                return new JsonResult(BadRequest("El email ya está registrado."));
            }

            // Hashear la contraseña
            if (usuario.password_hash != null)
            {
                usuario.password_hash = _passwordHashService.HashPassword(usuario.password_hash);
            }
            else
            {
                return new JsonResult(BadRequest("La contraseña no puede estar vacía."));
            }

            _context.Usuarios.Add(usuario);
            _context.SaveChanges();
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

            _context.Entry(usuarioInDb).CurrentValues.SetValues(usuario);
            _context.SaveChanges();
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
            _context.Usuarios.Remove(result);
            _context.SaveChanges();
            return new JsonResult(NoContent());
        }

        // GetAll
        [HttpGet]
        public JsonResult GetAll()
        {
            var result = _context.Usuarios.ToList();
            return new JsonResult(Ok(result));
        }

        // Registrar
        [HttpPost]
        public JsonResult Registrar(Usuario model)
        {
            var usuario = _context.Usuarios.FirstOrDefault(u => u.email == model.email);
            if (usuario != null)
            {
                return new JsonResult(BadRequest("El email ya está registrado."));
            }

            // Encriptar la contraseña antes de guardarla
            if (model.password_hash != null)
            {
                model.password_hash = _passwordHashService.HashPassword(model.password_hash);
                _context.Usuarios.Add(model);
            }
            else
            {
                return new JsonResult(BadRequest("La contraseña no puede estar vacía."));
            }
            _context.SaveChanges();
            return new JsonResult(Ok(model));
        }

        [HttpPost]
        public ActionResult<Usuario> IniciarSesion([FromBody] LoginViewModel model)
        {
            try
            {
                var usuario = _context.Usuarios
                    .FirstOrDefault(u => (u.email ?? string.Empty).ToLower() == (model.Email ?? string.Empty).ToLower());

                if (usuario == null || usuario.password_hash == null ||
                    !_passwordHashService.VerifyPassword(model.Password, usuario.password_hash))
                {
                    return BadRequest(new { message = "Credenciales inválidas" });
                }

                usuario.ultimo_acceso = DateTime.Now;
                _context.SaveChanges();

                return Ok(usuario);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }
    }
}
