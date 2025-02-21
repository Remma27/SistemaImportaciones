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
            var url = _apiBaseUrl + "GetAll";
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Error al obtener las bodegas: {response.StatusCode}, {content}");
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            using JsonDocument document = JsonDocument.Parse(content);
            var root = document.RootElement;
            if (root.TryGetProperty("value", out JsonElement valueElement))
            {
                var bodegasJson = valueElement.GetRawText();
                var bodegas = JsonSerializer.Deserialize<IEnumerable<Empresa_Bodegas>>(bodegasJson, options);
                return bodegas ?? new List<Empresa_Bodegas>();
            }
            return new List<Empresa_Bodegas>();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"API call failed: {ex.Message}");
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
            Empresa_Bodegas? bodega = null;

            if (jsonDocument.RootElement.TryGetProperty("value", out JsonElement valueElement))
            {
                bodega = JsonSerializer.Deserialize<Empresa_Bodegas>(valueElement.GetRawText(), options);
            }
            else
            {
                bodega = JsonSerializer.Deserialize<Empresa_Bodegas>(content, options);
            }
            return bodega ?? throw new InvalidOperationException("Error al obtener la bodega");
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al obtener la bodega {id}: {ex.Message}", ex);
        }
    }

    public async Task<Empresa_Bodegas> CreateAsync(Empresa_Bodegas bodega)
    {
        var response = await _httpClient.PostAsJsonAsync(_apiBaseUrl + "Create", bodega);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Empresa_Bodegas>();
        return result ?? throw new InvalidOperationException("Error al crear la bodega");
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