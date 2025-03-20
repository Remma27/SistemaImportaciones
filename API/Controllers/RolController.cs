using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using API.Data;
using API.Models;
using API.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "Administrador")] // Solo administradores pueden gestionar roles
    public class RolController : ControllerBase
    {
        private readonly ApiContext _context;
        private readonly HistorialService _historialService;
        private readonly RolPermisoService _rolPermisoService;

        public RolController(ApiContext context, HistorialService historialService, RolPermisoService rolPermisoService)
        {
            _context = context;
            _historialService = historialService;
            _rolPermisoService = rolPermisoService;
        }

        // Obtener todos los roles
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var roles = await _context.Roles.ToListAsync();
                return Ok(roles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener los roles", error = ex.Message });
            }
        }

        // Obtener un rol específico
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var rol = await _context.Roles.FindAsync(id);
                if (rol == null)
                    return NotFound(new { message = "Rol no encontrado" });

                return Ok(rol);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener el rol", error = ex.Message });
            }
        }

        // Crear un nuevo rol
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Rol rol)
        {
            try
            {
                if (rol == null)
                    return BadRequest(new { message = "Datos de rol inválidos" });

                _context.Roles.Add(rol);
                await _context.SaveChangesAsync();

                _historialService.GuardarHistorial(
                    "CREAR",
                    rol,
                    "Roles",
                    $"Creación de rol: {rol.nombre}"
                );

                return CreatedAtAction(nameof(Get), new { id = rol.id }, rol);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al crear el rol", error = ex.Message });
            }
        }

        // Actualizar un rol existente
        [HttpPut]
        public async Task<IActionResult> Edit([FromBody] Rol rol)
        {
            try
            {
                if (rol == null || rol.id == 0)
                    return BadRequest(new { message = "Datos de rol inválidos" });

                var rolExistente = await _context.Roles.FindAsync(rol.id);
                if (rolExistente == null)
                    return NotFound(new { message = "Rol no encontrado" });

                // Guardar estado anterior
                _historialService.GuardarHistorial(
                    "ANTES_EDITAR",
                    rolExistente,
                    "Roles",
                    $"Estado anterior del rol {rolExistente.nombre}"
                );

                // Actualizar propiedades
                rolExistente.nombre = rol.nombre;
                rolExistente.descripcion = rol.descripcion;

                await _context.SaveChangesAsync();

                // Guardar estado nuevo
                _historialService.GuardarHistorial(
                    "DESPUES_EDITAR",
                    rolExistente,
                    "Roles",
                    $"Estado nuevo del rol {rolExistente.nombre}"
                );

                return Ok(rolExistente);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar el rol", error = ex.Message });
            }
        }

        // Eliminar un rol
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var rol = await _context.Roles.FindAsync(id);
                if (rol == null)
                    return NotFound(new { message = "Rol no encontrado" });

                // Verificar que no haya usuarios con este rol
                var usuariosConRol = await _context.Usuarios.CountAsync(u => u.rol_id == id);
                if (usuariosConRol > 0)
                    return BadRequest(new { message = $"No se puede eliminar el rol porque está asignado a {usuariosConRol} usuarios" });

                _historialService.GuardarHistorial(
                    "ELIMINAR",
                    rol,
                    "Roles",
                    $"Eliminación de rol: {rol.nombre}"
                );

                _context.Roles.Remove(rol);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al eliminar el rol", error = ex.Message });
            }
        }

        // Obtener todos los permisos
        [HttpGet]
        public async Task<IActionResult> GetAllPermisos()
        {
            try
            {
                var permisos = await _context.Permisos.ToListAsync();
                return Ok(permisos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener los permisos", error = ex.Message });
            }
        }

        // Asignar permisos a un rol
        [HttpPost]
        public async Task<IActionResult> AsignarPermisos([FromBody] AsignarPermisosViewModel model)
        {
            try
            {
                var rol = await _context.Roles.FindAsync(model.RolId);
                if (rol == null)
                    return NotFound(new { message = "Rol no encontrado" });

                try
                {
                    // Intentar eliminar las asignaciones anteriores
                    var permisosActuales = await _context.RolPermisos
                        .Where(rp => rp.rol_id == model.RolId)
                        .ToListAsync();

                    _context.RolPermisos.RemoveRange(permisosActuales);
                }
                catch (Exception ex)
                {
                    // Si falla aquí, probablemente la tabla no existe
                    if (ex.InnerException?.Message?.Contains("doesn't exist") == true)
                    {
                        return StatusCode(500, new { message = "Error: La tabla 'rol_permisos' no existe en la base de datos. Por favor, contacte al administrador del sistema." });
                    }
                    throw; // Relanzar si es otro tipo de error
                }

                // Crear nuevas asignaciones
                var nuevosPermisos = new List<RolPermiso>();
                foreach (var permisoId in model.PermisosIds)
                {
                    nuevosPermisos.Add(new RolPermiso
                    {
                        rol_id = model.RolId,
                        permiso_id = permisoId
                    });
                }

                _context.RolPermisos.AddRange(nuevosPermisos);
                await _context.SaveChangesAsync();

                _historialService.GuardarHistorial(
                    "ASIGNAR_PERMISOS",
                    new { RolId = model.RolId, Permisos = model.PermisosIds },
                    "RolPermisos",
                    $"Asignación de permisos al rol: {rol.nombre}"
                );

                return Ok(new { message = "Permisos asignados con éxito" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al asignar permisos", error = ex.Message });
            }
        }

        // Obtener permisos de un rol
        [HttpGet("{rolId}")]
        public async Task<IActionResult> GetPermisosPorRol(int rolId)
        {
            try
            {
                var permisos = await _rolPermisoService.ObtenerPermisosPorRolId(rolId);
                return Ok(permisos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener permisos del rol", error = ex.Message });
            }
        }
    }

    public class AsignarPermisosViewModel
    {
        public int RolId { get; set; }
        public List<int> PermisosIds { get; set; } = new List<int>();
    }
}
