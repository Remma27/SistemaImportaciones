using API.Models;
using System.Text.Json;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;

namespace Sistema_de_Gestion_de_Importaciones.Services;

public class ImportacionService : IImportacionService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    private readonly ILogger<ImportacionService> _logger;

    public ImportacionService(HttpClient httpClient, IConfiguration configuration, ILogger<ImportacionService> logger)
    {
        _httpClient = httpClient;
        // Change the API route to match the controller
        _apiBaseUrl = "api/Importaciones/";
        _logger = logger;
    }

    public async Task<IEnumerable<Importacion>> GetAllAsync()
    {
        try
        {
            var url = _apiBaseUrl + "GetAll";
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Error al obtener las importaciones: {response.StatusCode}, {content}");
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            using JsonDocument document = JsonDocument.Parse(content);
            var root = document.RootElement;
            if (root.TryGetProperty("value", out JsonElement valueElement))
            {
                var importacionesJson = valueElement.GetRawText();
                var importaciones = JsonSerializer.Deserialize<IEnumerable<Importacion>>(importacionesJson, options);
                return importaciones ?? new List<Importacion>();
            }
            return new List<Importacion>();
        }
        catch (Exception ex)
        {
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
            Importacion? importacion = null;
            if (document.RootElement.TryGetProperty("value", out JsonElement valueElement))
            {
                importacion = JsonSerializer.Deserialize<Importacion>(valueElement.GetRawText(), options);
            }
            else
            {
                importacion = JsonSerializer.Deserialize<Importacion>(content, options);
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
        var response = await _httpClient.PostAsJsonAsync(_apiBaseUrl + "Create", importacion);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Importacion>() ?? importacion;
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
