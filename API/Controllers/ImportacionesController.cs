using Microsoft.AspNetCore.Mvc;
using API.Models;
using API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class ImportacionesController : ControllerBase
    {
        private readonly ApiContext _context;
        public ImportacionesController(ApiContext context)
        {
            _context = context;
        }

        // Endpoint para crear una nueva Importacion
        [HttpPost]
        [Consumes("application/json")]
        public JsonResult Create([FromBody] Importacion importacion)
        {
            if (importacion.id != 0)
            {
                return new JsonResult(BadRequest("El id debe ser 0 para crear una nueva importacion."));
            }
            _context.Importaciones.Add(importacion);
            _context.SaveChanges();
            return new JsonResult(Ok(importacion));
        }

        // Endpoint para editar una Importacion existente
        [HttpPut]
        public JsonResult Edit(Importacion importacion)
        {
            if (importacion.id == 0)
            {
                return new JsonResult(BadRequest("Debe proporcionar un id válido para editar una importacion."));
            }
            var importacionInDb = _context.Importaciones.Find(importacion.id);
            if (importacionInDb == null)
            {
                return new JsonResult(NotFound());
            }
            _context.Entry(importacionInDb).CurrentValues.SetValues(importacion);
            _context.SaveChanges();
            return new JsonResult(Ok(importacion));
        }

        // Get
        [HttpGet]
        public JsonResult Get(int id)
        {
            var result = _context.Importaciones
                .Include(i => i.Barco)
                .FirstOrDefault(i => i.id == id);
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
            var result = _context.Importaciones.Find(id);
            if (result == null)
            {
                return new JsonResult(NotFound());
            }
            _context.Importaciones.Remove(result);
            _context.SaveChanges();
            return new JsonResult(NoContent());
        }

        // Endpoint para obtener todas las importaciones
        [HttpGet]
        public JsonResult GetAll()
        {
            var result = _context.Importaciones
                .Include(i => i.Barco)
                .ToList();
            return new JsonResult(Ok(result));
        }
    }
}
