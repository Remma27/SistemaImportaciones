using API.Models;
using System.Text.Json;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;

namespace Sistema_de_Gestion_de_Importaciones.Services
{
    public class EmpresaService : IEmpresaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;
        private readonly ILogger<EmpresaService> _logger;

        public EmpresaService(HttpClient httpClient, IConfiguration configuration, ILogger<EmpresaService> logger)
        {
            _httpClient = httpClient;
            _apiBaseUrl = "api/Empresa/";
            _logger = logger;
        }

        public async Task<IEnumerable<Empresa>> GetAllAsync()
        {
            try
            {
                _logger.LogInformation("Iniciando solicitud GetAllAsync");
                var url = _apiBaseUrl + "GetAll";

                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Respuesta recibida: Status={response.StatusCode}");
                // Log first 100 chars of content for debugging
                _logger.LogDebug($"Contenido de respuesta: {content.Substring(0, Math.Min(100, content.Length))}...");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Error del API: {response.StatusCode}, Content={content}");
                    throw new HttpRequestException($"API error: {response.StatusCode}");
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // Analyze JSON structure
                using JsonDocument document = JsonDocument.Parse(content);
                var root = document.RootElement;
                
                _logger.LogDebug($"JSON Root Kind: {root.ValueKind}");
                
                // Handle different JSON formats
                if (root.ValueKind == JsonValueKind.Array)
                {
                    // Direct array - our new format
                    _logger.LogInformation("Detectado formato de array directo");
                    var empresas = JsonSerializer.Deserialize<List<Empresa>>(content, options);
                    _logger.LogInformation($"Empresas deserializadas: {empresas?.Count ?? 0}");
                    return empresas ?? new List<Empresa>();
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    // Object with a property - check common patterns
                    if (root.TryGetProperty("value", out JsonElement valueElement))
                    {
                        _logger.LogInformation("Detectado formato con propiedad 'value'");
                        var empresasJson = valueElement.GetRawText();
                        var empresas = JsonSerializer.Deserialize<List<Empresa>>(empresasJson, options);
                        return empresas ?? new List<Empresa>();
                    }
                    else
                    {
                        // Try Newtonsoft as last resort
                        _logger.LogInformation("Intentando deserialización con Newtonsoft.Json");
                        try
                        {
                            var empresas = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Empresa>>(content);
                            if (empresas != null)
                            {
                                _logger.LogInformation($"Empresas deserializadas con Newtonsoft: {empresas.Count}");
                                return empresas;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error con Newtonsoft.Json");
                        }
                    }
                }

                _logger.LogWarning("No se pudo deserializar la respuesta en ningún formato conocido");
                return new List<Empresa>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetAllAsync");
                throw;
            }
        }

        public async Task<Empresa?> GetByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation($"Iniciando solicitud GetByIdAsync para id={id}");
                var url = _apiBaseUrl + $"Get?id={id}";

                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Respuesta recibida: Status={response.StatusCode}, Content={content}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Error del API: {response.StatusCode}, Content={content}");
                    throw new HttpRequestException($"API error: {response.StatusCode}");
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                using JsonDocument jsonDocument = JsonDocument.Parse(content);
                Empresa? empresa = null;

                if (jsonDocument.RootElement.TryGetProperty("value", out JsonElement valueElement))
                {
                    empresa = JsonSerializer.Deserialize<Empresa>(valueElement.GetRawText(), options);
                }
                else
                {
                    empresa = JsonSerializer.Deserialize<Empresa>(content, options);
                }

                return empresa;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener empresa con ID {id}");
                throw;
            }
        }

        public async Task<Empresa> CreateAsync(Empresa empresa)
        {
            var response = await _httpClient.PostAsJsonAsync(_apiBaseUrl + "Create", empresa);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Empresa>() ?? empresa;
        }

        public async Task<Empresa> UpdateAsync(int id, Empresa empresa)
        {
            try
            {
                _logger.LogInformation($"Actualizando empresa con ID {id}");
                var response = await _httpClient.PutAsJsonAsync(_apiBaseUrl + $"Edit", empresa);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Respuesta del servidor: {content}");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return await response.Content.ReadFromJsonAsync<Empresa>(options) ?? empresa;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar empresa con ID {id}");
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            var url = _apiBaseUrl + $"Delete?id={id}";
            _logger.LogInformation($"Eliminando empresa con URL: {url}");
            var response = await _httpClient.DeleteAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Error al eliminar: Status={response.StatusCode}. Detalles: {content}");
            }
        }
    }
}