using Microsoft.AspNetCore.Mvc;
using API.Models;
using API.Data;
using Microsoft.AspNetCore.Authorization;

namespace API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class BodegaController : ControllerBase
    {
        private readonly ApiContext _context;
        public BodegaController(ApiContext context)
        {
            _context = context;
        }

        // Endpoint para crear una nueva Bodega
        [HttpPost]
        [Consumes("application/json")]
        public JsonResult Create([FromBody] Empresa_Bodegas bodega)
        {

            if (bodega.id != 0)
            {
                return new JsonResult(BadRequest("El id debe ser 0 para crear una nueva bodega."));
            }

            _context.Empresa_Bodegas.Add(bodega);
            _context.SaveChanges();
            return new JsonResult(Ok(bodega));
        }

        // Endpoint para editar una Bodega existente
        [HttpPut]
        public JsonResult Edit(Empresa_Bodegas bodega)
        {
            var bodegaInDb = _context.Empresa_Bodegas.Find(bodega.id);
            if (bodegaInDb == null)
            {
                return new JsonResult(NotFound());
            }

            _context.Entry(bodegaInDb).CurrentValues.SetValues(bodega);
            _context.SaveChanges();
            return new JsonResult(Ok(bodega));
        }

        // Get
        [HttpGet]
        public JsonResult Get(int id)
        {
            var result = _context.Empresa_Bodegas.Find(id);
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
            var result = _context.Empresa_Bodegas.Find(id);
            if (result == null)
            {
                return new JsonResult(NotFound());
            }
            _context.Empresa_Bodegas.Remove(result);
            _context.SaveChanges();
            return new JsonResult(NoContent());
        }

        // GetAll
        [HttpGet]
        public JsonResult GetAll()
        {
            var result = _context.Empresa_Bodegas.ToList();
            return new JsonResult(Ok(result));
        }
    }
}
