using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using Sistema_de_Gestion_de_Importaciones.ViewModels;
using System.Net.Http.Json;

namespace Sistema_de_Gestion_de_Importaciones.Services
{
    public class HistorialService : IHistorialService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HistorialService> _logger;

        public HistorialService(HttpClient httpClient, IConfiguration configuration, ILogger<HistorialService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<HistorialResponseViewModel> ObtenerTodoAsync()
        {
            try
            {
                _logger.LogInformation("Obteniendo todos los registros de historial sin filtrado");
                var response = await _httpClient.GetAsync("api/Historial/GetAll");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Respuesta JSON recibida: {Length} bytes", content.Length);
                
                // First, check if the response is a JSON array or object
                if (string.IsNullOrEmpty(content))
                {
                    _logger.LogWarning("Respuesta JSON vacía");
                    return new HistorialResponseViewModel();
                }
                
                // Trim whitespace to ensure accurate checking
                content = content.Trim();
                
                try
                {
                    if (content.StartsWith("[") && content.EndsWith("]"))
                    {
                        // Response is an array - deserialize directly to List<HistorialViewModel>
                        _logger.LogInformation("Respuesta detectada como array JSON");
                        var registros = JsonConvert.DeserializeObject<List<HistorialViewModel>>(content);
                        
                        return new HistorialResponseViewModel
                        {
                            TotalRegistros = registros?.Count ?? 0,
                            Registros = registros ?? new List<HistorialViewModel>()
                        };
                    }
                    else
                    {
                        // Response is an object - try to deserialize to HistorialResponseViewModel
                        _logger.LogInformation("Respuesta detectada como objeto JSON");
                        var resultado = JsonConvert.DeserializeObject<HistorialResponseViewModel>(content);
                        
                        if (resultado != null)
                        {
                            _logger.LogInformation("Registros de historial obtenidos: {Count}", resultado.TotalRegistros);
                            
                            // Verify the IDs received for debugging
                            if (resultado.Registros != null && resultado.Registros.Any())
                            {
                                var ids = resultado.Registros.Select(r => r.Id).Take(10).ToList();
                                _logger.LogInformation("Primeros 10 IDs: {Ids}", string.Join(", ", ids));
                                
                                // Check if there's any record with ID=1
                                var tieneId1 = resultado.Registros.Any(r => r.Id == 1);
                                _logger.LogInformation("¿Contiene registro con ID=1? {TieneId1}", tieneId1);
                            }
                        }
                        
                        return resultado ?? new HistorialResponseViewModel();
                    }
                }
                catch (JsonSerializationException ex)
                {
                    _logger.LogError(ex, "Error en deserialización JSON: {Message}", ex.Message);
                    
                    // Try one more approach - use System.Text.Json as fallback
                    try
                    {
                        _logger.LogInformation("Intentando deserialización con System.Text.Json");
                        using var document = System.Text.Json.JsonDocument.Parse(content);
                        
                        if (document.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            var options = new System.Text.Json.JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            };
                            
                            var registros = System.Text.Json.JsonSerializer.Deserialize<List<HistorialViewModel>>(
                                content, options);
                                
                            return new HistorialResponseViewModel
                            {
                                TotalRegistros = registros?.Count ?? 0,
                                Registros = registros ?? new List<HistorialViewModel>()
                            };
                        }
                    }
                    catch (Exception innerEx)
                    {
                        _logger.LogError(innerEx, "Error en deserialización con System.Text.Json");
                    }
                    
                    // Return empty result if all deserialization attempts fail
                    return new HistorialResponseViewModel();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener historial");
                return new HistorialResponseViewModel();
            }
        }

        public async Task<HistorialViewModel> ObtenerPorIdAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/Historial/GetById?id={id}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var resultado = JsonConvert.DeserializeObject<HistorialViewModel>(content);
                return resultado ?? new HistorialViewModel();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener historial por ID {Id}", id);
                return new HistorialViewModel();
            }
        }

        public async Task<List<string>> ObtenerTablasDisponiblesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/Historial/GetTablasDisponibles");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var resultado = JsonConvert.DeserializeObject<List<string>>(content);
                return resultado ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tablas disponibles");
                return new List<string>();
            }
        }
    }
}
