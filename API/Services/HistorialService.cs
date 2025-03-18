using System.Text.Json;
using API.Data;
using API.Models;
using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;
using System.Security.Claims;
using Sistema_de_Gestion_de_Importaciones.Helpers;

namespace API.Services
{
    public class HistorialService
    {
        private readonly ApiContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<HistorialService> _logger;

        public HistorialService(ApiContext context, IHttpContextAccessor httpContextAccessor, ILogger<HistorialService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public void GuardarHistorial(string operacion, object entidad, string tabla, string? descripcion = null)
        {
            try
            {
                // Obtener el ID del usuario autenticado
                var usuarioId = ObtenerUsuarioAutenticado();
                
                // Mejorado: Configuración de opciones de JSON para evitar referencias circulares y formatear mejor
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    ReferenceHandler = ReferenceHandler.Preserve, // Maneja referencias circulares
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, // Ignora propiedades nulas
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase // Usa camelCase para las propiedades
                };

                // Serializa directamente a string
                string jsonData;
                try 
                {
                    jsonData = JsonSerializer.Serialize(entidad, options);
                }
                catch (Exception jsonEx)
                {
                    // Si falla la serialización, intentamos una versión simplificada
                    _logger.LogWarning(jsonEx, "Error en serialización JSON completa, intentando simplificada");
                    
                    // Convertimos manualmente a diccionario simple para evitar referencias circulares
                    var simpleProperties = new Dictionary<string, object>();
                    foreach (var prop in entidad.GetType().GetProperties())
                    {
                        if (prop.PropertyType.IsPrimitive || prop.PropertyType == typeof(string) || 
                            prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(decimal))
                        {
                            var value = prop.GetValue(entidad);
                            simpleProperties[prop.Name] = value ?? "null";
                        }
                    }
                    jsonData = JsonSerializer.Serialize(simpleProperties, options);
                }

                var historial = new HistorialCambios
                {
                    UsuarioId = usuarioId,
                    Tabla = tabla,
                    TipoOperacion = operacion,
                    DatosJSON = jsonData,
                    FechaHora = DateTime.Now,
                    Descripcion = descripcion
                    // Se eliminó la propiedad IPAddress
                };

                _context.HistorialCambios.Add(historial);
                _context.SaveChanges();

                _logger.LogInformation("Historial registrado: {Operacion} en {Tabla} por usuario {UsuarioId}", 
                    operacion, tabla, usuarioId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar historial: {Operacion} {Tabla} - {Error}", 
                    operacion, tabla, ex.Message);
            }
        }

        private int ObtenerUsuarioAutenticado()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                _logger.LogWarning("No se pudo acceder al HttpContext");
                return 1;
            }
            
            if (httpContext.User == null || httpContext.User.Identity == null || !httpContext.User.Identity.IsAuthenticated)
            {
                _logger.LogWarning("Usuario no autenticado");
                return 1;
            }

            // Intentar directamente con ClaimTypes.NameIdentifier primero
            var nameIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (nameIdClaim != null && int.TryParse(nameIdClaim.Value, out int nameIdValue))
            {
                _logger.LogDebug("ID obtenido de NameIdentifier: {UserId}", nameIdValue);
                return nameIdValue;
            }
            
            // Si falla, intentar con la extensión
            try
            {
                var userId = httpContext.User.GetUserId();
                if (userId > 0)
                {
                    _logger.LogDebug("ID de usuario obtenido de extensión: {UserId}", userId);
                    return userId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al obtener ID de usuario desde extensión");
            }

            // Si todavía no funciona, intentar con todos los claims posibles
            foreach (var claim in httpContext.User.Claims)
            {
                _logger.LogDebug("Claim disponible: {Type} = {Value}", claim.Type, claim.Value);
                
                if (claim.Type.EndsWith("nameidentifier", StringComparison.OrdinalIgnoreCase) || 
                    claim.Type.Contains("userid", StringComparison.OrdinalIgnoreCase) ||
                    claim.Type.EndsWith("id", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(claim.Value, out int id))
                    {
                        return id;
                    }
                }
            }

            _logger.LogWarning("No se pudo determinar el ID de usuario, usando valor por defecto");
            return 1;
        }
    }
}
