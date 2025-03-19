using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Services
{
    public class RolPermisoService
    {
        private readonly ApiContext _context;
        private readonly ILogger<RolPermisoService> _logger;

        public RolPermisoService(ApiContext context, ILogger<RolPermisoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Obtener todos los permisos de un rol
        public async Task<List<Permiso>> ObtenerPermisosPorRolId(int rolId)
        {
            try
            {
                return await _context.RolPermisos
                    .Where(rp => rp.rol_id == rolId)
                    .Include(rp => rp.Permiso)
                    .Select(rp => rp.Permiso!)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener permisos para el rol {RolId}", rolId);
                return new List<Permiso>();
            }
        }

        // Verificar si un rol tiene un permiso específico
        public async Task<bool> RolTienePermiso(int rolId, string permisoNombre)
        {
            try
            {
                return await _context.RolPermisos
                    .Where(rp => rp.rol_id == rolId)
                    .Include(rp => rp.Permiso)
                    .AnyAsync(rp => rp.Permiso != null && rp.Permiso.nombre == permisoNombre);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar permiso {Permiso} para el rol {RolId}", permisoNombre, rolId);
                return false;
            }
        }

        // Obtener lista de todos los roles
        public async Task<List<Rol>> ObtenerTodosLosRoles()
        {
            return await _context.Roles.ToListAsync();
        }

        // Obtener lista de todos los permisos
        public async Task<List<Permiso>> ObtenerTodosLosPermisos()
        {
            return await _context.Permisos.ToListAsync();
        }
    }
}
