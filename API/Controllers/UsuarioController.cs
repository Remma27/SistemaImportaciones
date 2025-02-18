using Microsoft.AspNetCore.Mvc;
using API.Models;
using API.Data;

namespace API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class UsuarioController : ControllerBase
    {
        private readonly ApiContext _context;
        public UsuarioController(ApiContext context)
        {
            _context = context;
        }

        // Endpoint para crear un nuevo Usuario
        [HttpPost]
        public JsonResult Create(Usuario usuario)
        {
            if (usuario.id != 0)
            {
                return new JsonResult(BadRequest("El id debe ser 0 para crear un nuevo usuario."));
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

        // IniciarSesion
        [HttpPost]
        public JsonResult IniciarSesion(Usuario model)
        {
            var usuario = _context.Usuarios.FirstOrDefault(u => u.email == model.email && u.password_hash == model.password_hash);
            if (usuario == null)
            {
                return new JsonResult(NotFound());
            }
            return new JsonResult(Ok(usuario));
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
            _context.Usuarios.Add(model);
            _context.SaveChanges();
            return new JsonResult(Ok(model));
        }
    }
}
