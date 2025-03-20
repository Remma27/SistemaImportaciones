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

        // Helper method to get Costa Rica current time
        private DateTime GetCostaRicaTime()
        {
            try
            {
                var costaRicaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, costaRicaTimeZone);
            }
            catch (Exception)
            {
                // Fallback: Costa Rica is UTC-6, so manually adjust if timezone not found
                return DateTime.UtcNow.AddHours(-6);
            }
        }

        public void GuardarHistorial(string operacion, object entidad, string tabla, string? descripcion = null)
        {
            try
            {
                // Obtener el ID del usuario autenticado
                var usuarioId = ObtenerUsuarioAutenticado();

                // Si no se pudo obtener un ID válido, usar el usuario del sistema (1)
                if (usuarioId <= 0)
                {
                    _logger.LogWarning("No se pudo obtener un ID de usuario válido, usando usuario del sistema (ID=1)");
                    usuarioId = 1;
                }

                // Registrar explícitamente el ID que se está usando
                _logger.LogInformation("Registrando historial con UsuarioId: {UsuarioId}", usuarioId);

                // Mejorado: Configuración de opciones de JSON para evitar referencias circulares y formatear mejor
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    ReferenceHandler = ReferenceHandler.IgnoreCycles, // Cambiado a IgnoreCycles en lugar de Preserve
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
                    FechaHora = GetCostaRicaTime(), // Updated to use Costa Rica time
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
            if (httpContext == null || httpContext.User == null || httpContext.User.Identity == null || !httpContext.User.Identity.IsAuthenticated)
            {
                _logger.LogWarning("Usuario no autenticado");
                return 0; // Usuario no autenticado
            }

            // Usar el helper directamente
            int userId = httpContext.User.GetUserId();

            // Si el helper devuelve 0, intentar obtener el ID directamente desde las claims
            if (userId <= 0)
            {
                // Obtener el ID directamente desde ClaimTypes.NameIdentifier
                var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out userId))
                {
                    _logger.LogInformation("ID de usuario extraído correctamente: {UserId}", userId);
                    return userId;
                }

                // Registrar todas las claims para depuración
                var claims = httpContext.User.Claims.ToList();
                _logger.LogWarning("Claims disponibles: {Claims}",
                    string.Join(", ", claims.Select(c => $"{c.Type}={c.Value}")));

                _logger.LogWarning("No se pudo determinar el ID de usuario desde claims");
                return 0;
            }

            return userId > 0 ? userId : 1; // Si el helper devuelve 0, usar el usuario del sistema
        }
    }
}