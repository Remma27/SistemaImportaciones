using API.Models;
using System.Text.Json;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;

namespace Sistema_de_Gestion_de_Importaciones.Services;

public class BarcoService : IBarcoService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;
    private readonly ILogger<BarcoService> _logger;

    public BarcoService(HttpClient httpClient, IConfiguration configuration, ILogger<BarcoService> logger)
    {
        _httpClient = httpClient;
        _apiBaseUrl = "api/Barco/";
        _logger = logger;
    }

    public async Task<IEnumerable<Barco>> GetAllAsync()
    {
        try
        {
            _logger.LogInformation("Iniciando solicitud GetAllAsync para barcos");
            var url = _apiBaseUrl + "GetAll";
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"Respuesta recibida: Status={response.StatusCode}");
            _logger.LogDebug($"Contenido de respuesta: {content.Substring(0, Math.Min(100, content.Length))}...");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Error del API: {response.StatusCode}, Content={content}");
                throw new HttpRequestException($"Error al obtener los barcos: {response.StatusCode}, {content}");
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
                var barcos = JsonSerializer.Deserialize<List<Barco>>(content, options);
                _logger.LogInformation($"Barcos deserializados: {barcos?.Count ?? 0}");
                return barcos ?? new List<Barco>();
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("value", out JsonElement valueElement))
                {
                    _logger.LogInformation("Detectado formato con propiedad 'value'");
                    var barcosJson = valueElement.GetRawText();
                    var barcos = JsonSerializer.Deserialize<List<Barco>>(barcosJson, options);
                    return barcos ?? new List<Barco>();
                }
                else
                {
                    _logger.LogInformation("Intentando deserialización con Newtonsoft.Json");
                    try
                    {
                        var barcos = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Barco>>(content);
                        if (barcos != null)
                        {
                            _logger.LogInformation($"Barcos deserializados con Newtonsoft: {barcos.Count}");
                            return barcos;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error con Newtonsoft.Json");
                    }
                }
            }

            _logger.LogWarning("No se pudo deserializar la respuesta en ningún formato conocido");
            return new List<Barco>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error al obtener los barcos");
            throw new Exception($"Error al obtener los barcos: {ex.Message}", ex);
        }
    }

    public async Task<Barco?> GetByIdAsync(int id)
    {
        try
        {
            _logger.LogInformation($"Obteniendo barco con ID={id}");
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

            using JsonDocument document = JsonDocument.Parse(content);
            var root = document.RootElement;
            
            _logger.LogDebug($"JSON Root Kind: {root.ValueKind}");
            
            Barco? barco = null;
            
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("value", out JsonElement valueElement))
                {
                    barco = JsonSerializer.Deserialize<Barco>(valueElement.GetRawText(), options);
                }
                else
                {
                    barco = JsonSerializer.Deserialize<Barco>(content, options);
                }
            }
            
            return barco;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error al obtener el barco con ID {id}");
            throw;
        }
    }

    public async Task<Barco> CreateAsync(Barco barco)
    {
        try
        {
            _logger.LogInformation($"Iniciando solicitud CreateAsync para barco");
            var response = await _httpClient.PostAsJsonAsync(_apiBaseUrl + "Create", barco);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"Respuesta recibida: Status={response.StatusCode}");
            _logger.LogDebug($"Contenido de respuesta: {content}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Error del API: {response.StatusCode}, Content={content}");
                throw new HttpRequestException($"Error al crear el barco: {response.StatusCode}, {content}");
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            using JsonDocument document = JsonDocument.Parse(content);
            Barco? result = null;

            if (document.RootElement.TryGetProperty("value", out JsonElement valueElement))
            {
                result = JsonSerializer.Deserialize<Barco>(valueElement.GetRawText(), options);
            }
            else
            {
                result = JsonSerializer.Deserialize<Barco>(content, options);
            }

            return result ?? barco;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear el barco");
            throw new Exception($"Error al crear el barco: {ex.Message}", ex);
        }
    }

    public async Task<Barco> UpdateAsync(int id, Barco barco)
    {
        try
        {
            _logger.LogInformation($"Actualizando barco con ID={id}");
            var response = await _httpClient.PutAsJsonAsync(_apiBaseUrl + $"Edit", barco);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"Respuesta del servidor: {content}");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return await response.Content.ReadFromJsonAsync<Barco>(options) ?? barco;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error al actualizar el barco con ID {id}");
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        var url = _apiBaseUrl + $"Delete?id={id}";
        _logger.LogInformation($"Eliminando barco con ID={id}");
        var response = await _httpClient.DeleteAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Error al eliminar el barco con ID={id}");
            throw new HttpRequestException($"Error al eliminar el barco con ID={id}");
        }
    }
}

