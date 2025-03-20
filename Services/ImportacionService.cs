using API.Models;
using System.Text.Json;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using System.Text;

namespace Sistema_de_Gestion_de_Importaciones.Services;

public class ImportacionService : IImportacionService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;
    private readonly ILogger<ImportacionService> _logger;

    public ImportacionService(HttpClient httpClient, IConfiguration configuration, ILogger<ImportacionService> logger)
    {
        _httpClient = httpClient;
        _apiBaseUrl = "api/Importaciones/";
        _logger = logger;
    }

    public async Task<IEnumerable<Importacion>> GetAllAsync()
    {
        try
        {
            _logger.LogInformation("Iniciando solicitud GetAllAsync para importaciones");
            var url = _apiBaseUrl + "GetAll";
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation($"Respuesta recibida: Status={response.StatusCode}");
            _logger.LogDebug($"Contenido de respuesta: {content.Substring(0, Math.Min(100, content.Length))}...");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Error del API: {response.StatusCode}, Content={content}");
                throw new HttpRequestException($"Error al obtener las importaciones: {response.StatusCode}, {content}");
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
                var importaciones = JsonSerializer.Deserialize<List<Importacion>>(content, options);
                _logger.LogInformation($"Importaciones deserializadas: {importaciones?.Count ?? 0}");
                return importaciones ?? new List<Importacion>();
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                // Object with a property - check common patterns
                if (root.TryGetProperty("value", out JsonElement valueElement))
                {
                    _logger.LogInformation("Detectado formato con propiedad 'value'");
                    var importacionesJson = valueElement.GetRawText();
                    var importaciones = JsonSerializer.Deserialize<List<Importacion>>(importacionesJson, options);
                    return importaciones ?? new List<Importacion>();
                }
                else
                {
                    // Try Newtonsoft as last resort
                    _logger.LogInformation("Intentando deserialización con Newtonsoft.Json");
                    try
                    {
                        var importaciones = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Importacion>>(content);
                        if (importaciones != null)
                        {
                            _logger.LogInformation($"Importaciones deserializadas con Newtonsoft: {importaciones.Count}");
                            return importaciones;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error con Newtonsoft.Json");
                    }
                }
            }

            _logger.LogWarning("No se pudo deserializar la respuesta en ningún formato conocido");
            return new List<Importacion>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener las importaciones");
            throw new Exception($"Error al obtener las importaciones: {ex.Message}", ex);
        }
    }

    public async Task<Importacion> GetByIdAsync(int id)
    {
        try
        {
            _logger.LogInformation($"Obteniendo importación con ID={id}");
            var url = _apiBaseUrl + $"Get?id={id}";

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"Respuesta recibida: Status={response.StatusCode}");
            _logger.LogDebug($"Contenido de respuesta: {content.Substring(0, Math.Min(100, content.Length))}...");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Error al obtener la importación {id}: {response.StatusCode}, {content}");
                throw new HttpRequestException($"Error al obtener la importación {id}: {response.StatusCode}, {content}");
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // Analyze JSON structure
            using JsonDocument document = JsonDocument.Parse(content);
            var root = document.RootElement;
            
            _logger.LogDebug($"JSON Root Kind: {root.ValueKind}");
            
            Importacion? importacion = null;
            
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("value", out JsonElement valueElement))
                {
                    importacion = JsonSerializer.Deserialize<Importacion>(valueElement.GetRawText(), options);
                }
                else
                {
                    // Direct object - new format
                    importacion = JsonSerializer.Deserialize<Importacion>(content, options);
                }
            }
            
            return importacion ?? throw new InvalidOperationException($"Error al obtener la importación {id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error al obtener la importación {id}");
            throw new Exception($"Error al obtener la importación {id}: {ex.Message}", ex);
        }
    }

    public async Task<Importacion> CreateAsync(Importacion importacion)
    {
        try
        {
            _logger.LogInformation($"Creating importacion: {JsonSerializer.Serialize(importacion)}");
            
            // Add detailed request logging
            var requestContent = new StringContent(
                JsonSerializer.Serialize(importacion), 
                Encoding.UTF8, 
                "application/json");
            
            _logger.LogInformation($"Request to API: {_apiBaseUrl}Create");
            
            // Use HttpClient directly for more control
            var response = await _httpClient.PostAsync(_apiBaseUrl + "Create", requestContent);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation($"API response status: {(int)response.StatusCode} {response.StatusCode}");
            _logger.LogInformation($"API response content: {responseContent}");
            
            // Save the raw response for debugging
            System.IO.File.WriteAllText(
                Path.Combine(Path.GetTempPath(), $"importacion_response_{DateTime.Now:yyyyMMdd_HHmmss}.json"), 
                responseContent);
                
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"API error: {response.StatusCode}, Content: {responseContent}");
                throw new HttpRequestException($"Error from API: {response.StatusCode}, Details: {responseContent}");
            }
            
            // Try to parse the response - with extensive error handling
            try {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                
                using var document = JsonDocument.Parse(responseContent);
                _logger.LogInformation($"Parsed JSON document with root kind: {document.RootElement.ValueKind}");
                
                // Try to extract the created importacion
                if (document.RootElement.TryGetProperty("value", out var valueElement))
                {
                    var importacionJson = valueElement.GetRawText();
                    _logger.LogInformation($"Found value element: {importacionJson}");
                    
                    var result = JsonSerializer.Deserialize<Importacion>(importacionJson, options);
                    _logger.LogInformation($"Created importation with ID: {result?.id ?? 0}");
                    return result ?? importacion;
                }
                else
                {
                    _logger.LogWarning("No 'value' property found in response");
                    var result = JsonSerializer.Deserialize<Importacion>(responseContent, options);
                    return result ?? importacion;
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JSON deserialization error when creating importation");
                throw new Exception($"Error processing API response: {jsonEx.Message}", jsonEx);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating importation");
            throw;
        }
    }

    public async Task<Importacion> UpdateAsync(int id, Importacion importacion)
    {
        try
        {
            _logger.LogInformation($"Actualizando importación con ID={id}");
            var response = await _httpClient.PutAsJsonAsync(_apiBaseUrl + $"Edit", importacion);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"Respuesta recibida: Status={response.StatusCode}, Content={content}");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return await response.Content.ReadFromJsonAsync<Importacion>(options) ?? importacion;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error al actualizar la importación {id}");
            throw new Exception($"Error al actualizar la importación {id}: {ex.Message}", ex);
        }
    }

    public async Task DeleteAsync(int id)
    {
        var url = _apiBaseUrl + $"Delete?id={id}";
        _logger.LogInformation($"Eliminando importacion con ID={id}");
        var response = await _httpClient.DeleteAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Error al eliminar la importacion con ID={id}");
            throw new HttpRequestException($"Error al eliminar la importacion con ID={id}");
        }
    }
}
