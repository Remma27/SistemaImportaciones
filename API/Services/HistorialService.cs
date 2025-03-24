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

        private DateTime GetCostaRicaTime()
        {
            try
            {
                var costaRicaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, costaRicaTimeZone);
            }
            catch (Exception)
            {
                return DateTime.UtcNow.AddHours(-6);
            }
        }

        public void GuardarHistorial(string operacion, object entidad, string tabla, string? descripcion = null)
        {
            try
            {
                var usuarioId = ObtenerUsuarioAutenticado();

                if (usuarioId <= 0)
                {
                    _logger.LogWarning("No se pudo obtener un ID de usuario válido, usando usuario del sistema (ID=1)");
                    usuarioId = 1;
                }

                _logger.LogInformation("Registrando historial con UsuarioId: {UsuarioId}", usuarioId);

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    ReferenceHandler = ReferenceHandler.IgnoreCycles, 
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                };

                string jsonData;
                try
                {
                    jsonData = JsonSerializer.Serialize(entidad, options);
                }
                catch (Exception jsonEx)
                {
                    _logger.LogWarning(jsonEx, "Error en serialización JSON completa, intentando simplificada");

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
                    FechaHora = GetCostaRicaTime(), 
                    Descripcion = descripcion
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
                return 0; 
            }

            int userId = httpContext.User.GetUserId();

            if (userId <= 0)
            {
                var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out userId))
                {
                    _logger.LogInformation("ID de usuario extraído correctamente: {UserId}", userId);
                    return userId;
                }

                var claims = httpContext.User.Claims.ToList();
                _logger.LogWarning("Claims disponibles: {Claims}",
                    string.Join(", ", claims.Select(c => $"{c.Type}={c.Value}")));

                _logger.LogWarning("No se pudo determinar el ID de usuario desde claims");
                return 0;
            }

            return userId > 0 ? userId : 1;
        }
    }
}