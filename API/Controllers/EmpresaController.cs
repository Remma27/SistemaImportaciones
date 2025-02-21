using Microsoft.AspNetCore.Mvc;
using API.Models;
using API.Data;

namespace API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class EmpresaController : ControllerBase
    {
        private readonly ApiContext _context;
        public EmpresaController(ApiContext context)
        {
            _context = context;
        }

        // Endpoint para crear una nueva Empresa
        [HttpPost]
        [Consumes("application/json")]
        public JsonResult Create([FromBody] Empresa empresa)
        {
            if (empresa.id_empresa != 0)
            {
                return new JsonResult(BadRequest("El id debe ser 0 para crear una nueva empresa."));
            }
            _context.Empresas.Add(empresa);
            _context.SaveChanges();
            return new JsonResult(Ok(empresa));
        }

        // Endpoint para editar una Empresa existente
        [HttpPut]
        public JsonResult Edit(Empresa empresa)
        {
            if (empresa.id_empresa == 0)
            {
                return new JsonResult(BadRequest("Debe proporcionar un id válido para editar una empresa."));
            }
            var empresaInDb = _context.Empresas.Find(empresa.id_empresa);
            if (empresaInDb == null)
            {
                return new JsonResult(NotFound());
            }
            _context.Entry(empresaInDb).CurrentValues.SetValues(empresa);
            _context.SaveChanges();
            return new JsonResult(Ok(empresa));
        }

        // Get
        [HttpGet]
        public JsonResult Get(int id)
        {
            var result = _context.Empresas.Find(id);
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
            var result = _context.Empresas.Find(id);
            if (result == null)
            {
                return new JsonResult(NotFound());
            }
            _context.Empresas.Remove(result);
            _context.SaveChanges();
            return new JsonResult(NoContent());
        }

        // Get All
        [HttpGet]
        public JsonResult GetAll()
        {
            var result = _context.Empresas.ToList();
            return new JsonResult(Ok(result));
        }
    }
}
