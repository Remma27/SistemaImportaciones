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

            using JsonDocument document = JsonDocument.Parse(content);
            var root = document.RootElement;
            
            _logger.LogDebug($"JSON Root Kind: {root.ValueKind}");
            
            if (root.ValueKind == JsonValueKind.Array)
            {
                _logger.LogInformation("Detectado formato de array directo");
                var importaciones = JsonSerializer.Deserialize<List<Importacion>>(content, options);
                _logger.LogInformation($"Importaciones deserializadas: {importaciones?.Count ?? 0}");
                return importaciones ?? new List<Importacion>();
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("value", out JsonElement valueElement))
                {
                    _logger.LogInformation("Detectado formato con propiedad 'value'");
                    var importacionesJson = valueElement.GetRawText();
                    var importaciones = JsonSerializer.Deserialize<List<Importacion>>(importacionesJson, options);
                    return importaciones ?? new List<Importacion>();
                }
                else
                {
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
            
            var requestContent = new StringContent(
                JsonSerializer.Serialize(importacion), 
                Encoding.UTF8, 
                "application/json");
            
            _logger.LogInformation($"Request to API: {_apiBaseUrl}Create");
            
            var response = await _httpClient.PostAsync(_apiBaseUrl + "Create", requestContent);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation($"API response status: {(int)response.StatusCode} {response.StatusCode}");
            _logger.LogInformation($"API response content: {responseContent}");
            
            System.IO.File.WriteAllText(
                Path.Combine(Path.GetTempPath(), $"importacion_response_{DateTime.Now:yyyyMMdd_HHmmss}.json"), 
                responseContent);
                
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"API error: {response.StatusCode}, Content: {responseContent}");
                throw new HttpRequestException($"Error from API: {response.StatusCode}, Details: {responseContent}");
            }
            
            try {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                
                using var document = JsonDocument.Parse(responseContent);
                _logger.LogInformation($"Parsed JSON document with root kind: {document.RootElement.ValueKind}");
                
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
        try
        {
            var url = _apiBaseUrl + $"Delete?id={id}";
            _logger.LogInformation($"Eliminando importación con ID={id}");
            
            var response = await _httpClient.DeleteAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Error al eliminar importación: Status={response.StatusCode}, Content={content}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    try
                    {
                        using (JsonDocument doc = JsonDocument.Parse(content))
                        {
                            if (doc.RootElement.TryGetProperty("message", out JsonElement messageElement))
                            {
                                string mensaje = messageElement.GetString() ?? "No se puede eliminar la importación";
                                
                                if (mensaje.Contains("movimientos") || mensaje.Contains("asociados"))
                                {
                                    _logger.LogWarning($"Importación con ID={id} tiene movimientos asociados");
                                    throw new InvalidOperationException(mensaje);
                                }
                                
                                throw new InvalidOperationException(mensaje);
                            }
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, $"Error al analizar la respuesta JSON: {content}");
                    }
                    
                    if (content.Contains("movimientos") || content.Contains("asociados"))
                    {
                        throw new InvalidOperationException("No se puede eliminar esta importación porque tiene movimientos asociados.");
                    }
                }
                
                throw new HttpRequestException($"Error al eliminar importación: {response.StatusCode}, {content}");
            }
            
            _logger.LogInformation($"Importación con ID={id} eliminada exitosamente");
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error inesperado al eliminar la importación con ID={id}");
            throw new Exception($"Error al eliminar la importación: {ex.Message}", ex);
        }
    }
}
