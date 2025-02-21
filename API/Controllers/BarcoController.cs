using Microsoft.AspNetCore.Mvc;
using API.Models;
using API.Data;

namespace API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class BarcoController : ControllerBase
    {
        private readonly ApiContext _context;

        public BarcoController(ApiContext context)
        {
            _context = context;
        }

        // Endpoint para crear un nuevo Barco
        [HttpPost]
        [Consumes("application/json")]
        public JsonResult Create([FromBody] Barco barco)
        {
            if (barco.id != 0)
            {
                return new JsonResult(BadRequest("El id debe ser 0 para crear un nuevo barco."));
            }
            _context.Barcos.Add(barco);
            _context.SaveChanges();
            return new JsonResult(Ok(barco));
        }

        // Endpoint para editar un Barco existente
        [HttpPut]
        public JsonResult Edit(Barco barco)
        {
            var barcoInDb = _context.Barcos.Find(barco.id);
            if (barcoInDb == null)
            {
                return new JsonResult(NotFound());
            }

            _context.Entry(barcoInDb).CurrentValues.SetValues(barco);
            _context.SaveChanges();
            return new JsonResult(Ok(barco));
        }

        // Get
        [HttpGet]
        public JsonResult Get(int id)
        {
            var result = _context.Barcos.Find(id);
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
            var result = _context.Barcos.Find(id);
            if (result == null)
            {
                return new JsonResult(NotFound());
            }
            _context.Barcos.Remove(result);
            _context.SaveChanges();
            return new JsonResult(NoContent());
        }

        // GetAll
        [HttpGet]
        public JsonResult GetAll()
        {
            var result = _context.Barcos.ToList();
            return new JsonResult(Ok(result));
        }
    }
}

