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
        private bool _tablaRolPermisosExiste = true; 

        public RolPermisoService(ApiContext context, ILogger<RolPermisoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        private async Task<bool> VerificarTablaRolPermisosExiste()
        {
            if (!_tablaRolPermisosExiste) return false; 
            
            try
            {
                await _context.RolPermisos.Take(1).CountAsync();
                return true;
            }
            catch (Exception ex)
            {
                if (ex.InnerException?.Message?.Contains("doesn't exist") == true)
                {
                    _tablaRolPermisosExiste = false;
                    _logger.LogError("La tabla 'rol_permisos' no existe en la base de datos. Las consultas relacionadas devolverán resultados vacíos.");
                    return false;
                }
                throw;
            }
        }

        public async Task<List<Permiso>> ObtenerPermisosPorRolId(int rolId)
        {
            try
            {
                if (!await VerificarTablaRolPermisosExiste())
                {
                    _logger.LogWarning("No se pudieron obtener permisos para rol {RolId} porque la tabla 'rolpermisos' no existe", rolId);
                    return new List<Permiso>();
                }

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

        public async Task<bool> RolTienePermiso(int rolId, string permisoNombre)
        {
            try
            {
                if (!await VerificarTablaRolPermisosExiste())
                {
                    _logger.LogWarning("No se pudo verificar permiso {Permiso} para rol {RolId} porque la tabla 'rolpermisos' no existe", permisoNombre, rolId);
                    
                    var rol = await _context.Roles.FindAsync(rolId);
                    if (rol?.nombre?.Equals("Administrador", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        _logger.LogInformation("Rol Administrador: permiso {Permiso} asumido como verdadero aunque la tabla 'rolpermisos' no existe", permisoNombre);
                        return true;
                    }
                    
                    return false;
                }

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

        public async Task<List<Rol>> ObtenerTodosLosRoles()
        {
            try
            {
                return await _context.Roles.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los roles");
                return new List<Rol>();
            }
        }

        public async Task<List<Permiso>> ObtenerTodosLosPermisos()
        {
            try
            {
                return await _context.Permisos.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los permisos");
                return new List<Permiso>();
            }
        }
    }
}
