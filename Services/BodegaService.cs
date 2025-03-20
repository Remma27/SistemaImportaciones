using API.Models;
using System.Text.Json;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;

namespace Sistema_de_Gestion_de_Importaciones.Services;

public class BodegaService : IBodegaService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;
    private readonly ILogger<BodegaService> _logger;

    public BodegaService(HttpClient httpClient, IConfiguration configuration, ILogger<BodegaService> logger)
    {
        _httpClient = httpClient;
        _apiBaseUrl = "api/Bodega/";
        _logger = logger;
    }

    public async Task<IEnumerable<Empresa_Bodegas>> GetAllAsync()
    {
        try
        {
            _logger.LogInformation("Iniciando solicitud GetAllAsync para bodegas");
            var url = _apiBaseUrl + "GetAll";
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"Respuesta recibida: Status={response.StatusCode}");
            _logger.LogDebug($"Contenido de respuesta: {content.Substring(0, Math.Min(100, content.Length))}...");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Error del API: {response.StatusCode}, Content={content}");
                throw new HttpRequestException($"Error al obtener las bodegas: {response.StatusCode}, {content}");
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
                // Direct array - new format
                _logger.LogInformation("Detectado formato de array directo");
                var bodegas = JsonSerializer.Deserialize<List<Empresa_Bodegas>>(content, options);
                _logger.LogInformation($"Bodegas deserializadas: {bodegas?.Count ?? 0}");
                return bodegas ?? new List<Empresa_Bodegas>();
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                // Object with a property - check common patterns
                if (root.TryGetProperty("value", out JsonElement valueElement))
                {
                    _logger.LogInformation("Detectado formato con propiedad 'value'");
                    var bodegasJson = valueElement.GetRawText();
                    var bodegas = JsonSerializer.Deserialize<List<Empresa_Bodegas>>(bodegasJson, options);
                    return bodegas ?? new List<Empresa_Bodegas>();
                }
                else
                {
                    // Try Newtonsoft as last resort
                    _logger.LogInformation("Intentando deserialización con Newtonsoft.Json");
                    try
                    {
                        var bodegas = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Empresa_Bodegas>>(content);
                        if (bodegas != null)
                        {
                            _logger.LogInformation($"Bodegas deserializadas con Newtonsoft: {bodegas.Count}");
                            return bodegas;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error con Newtonsoft.Json");
                    }
                }
            }

            _logger.LogWarning("No se pudo deserializar la respuesta en ningún formato conocido");
            return new List<Empresa_Bodegas>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error al obtener las bodegas");
            throw new Exception($"Error al obtener las bodegas: {ex.Message}", ex);
        }
    }

    public async Task<Empresa_Bodegas> GetByIdAsync(int id)
    {
        try
        {
            _logger.LogInformation($"Iniciando solicitud GetByIdAsync para la bodega {id}");
            var url = _apiBaseUrl + $"Get?id={id}";
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"Respuesta recibida: Status={response.StatusCode}");
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
            
            Empresa_Bodegas? bodega = null;
            
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("value", out JsonElement valueElement))
                {
                    bodega = JsonSerializer.Deserialize<Empresa_Bodegas>(valueElement.GetRawText(), options);
                }
                else
                {
                    // Direct object - new format
                    bodega = JsonSerializer.Deserialize<Empresa_Bodegas>(content, options);
                }
            }
            
            return bodega ?? throw new InvalidOperationException("Error al obtener la bodega");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error al obtener la bodega {id}");
            throw new Exception($"Error al obtener la bodega {id}: {ex.Message}", ex);
        }
    }

    public async Task<Empresa_Bodegas> CreateAsync(Empresa_Bodegas bodega)
    {
        try
        {
            _logger.LogInformation($"Iniciando solicitud CreateAsync para bodega");
            var response = await _httpClient.PostAsJsonAsync(_apiBaseUrl + "Create", bodega);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"Respuesta recibida: Status={response.StatusCode}");
            _logger.LogDebug($"Contenido de respuesta: {content}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Error del API: {response.StatusCode}, Content={content}");
                throw new HttpRequestException($"Error al crear la bodega: {response.StatusCode}, {content}");
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            using JsonDocument document = JsonDocument.Parse(content);
            Empresa_Bodegas? result = null;

            if (document.RootElement.TryGetProperty("value", out JsonElement valueElement))
            {
                result = JsonSerializer.Deserialize<Empresa_Bodegas>(valueElement.GetRawText(), options);
            }
            else
            {
                result = JsonSerializer.Deserialize<Empresa_Bodegas>(content, options);
            }

            return result ?? throw new InvalidOperationException("Error al crear la bodega: respuesta nula");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear la bodega");
            throw new Exception($"Error al crear la bodega: {ex.Message}", ex);
        }
    }

    public async Task<Empresa_Bodegas> UpdateAsync(int id, Empresa_Bodegas bodega)
    {
        try
        {
            _logger.LogInformation($"Iniciando solicitud UpdateAsync para la bodega {id}");
            var response = await _httpClient.PutAsJsonAsync(_apiBaseUrl + $"Edit", bodega);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"Respuesta recibida: Status={response.StatusCode}, Content={content}");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return await response.Content.ReadFromJsonAsync<Empresa_Bodegas>(options) ?? bodega;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al actualizar la bodega: {ex.Message}", ex);
        }
    }

    public async Task DeleteAsync(int id)
    {
        var url = _apiBaseUrl + $"Delete?id={id}";
        _logger.LogInformation($"Iniciando solicitud DeleteAsync para la bodega {id}");
        var response = await _httpClient.DeleteAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Error al eliminar la bodega: Status={response.StatusCode}. Detalles: {content}");
        }
    }
}