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
            var url = _apiBaseUrl + "GetAll";
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Error al obtener los barcos: {response.StatusCode}, {content}");
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            using JsonDocument document = JsonDocument.Parse(content);
            var root = document.RootElement;
            if (root.TryGetProperty("value", out JsonElement valueElement))
            {
                var barcosJson = valueElement.GetRawText();
                var barcos = JsonSerializer.Deserialize<IEnumerable<Barco>>(barcosJson, options);
                return barcos ?? new List<Barco>();
            }
            return new List<Barco>();
        }
        catch (HttpRequestException ex)
        {
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
            Barco? barco = null;

            if (document.RootElement.TryGetProperty("value", out JsonElement valueElement))
            {
                barco = JsonSerializer.Deserialize<Barco>(valueElement.GetRawText(), options);
            }
            else
            {
                barco = JsonSerializer.Deserialize<Barco>(content, options);
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
        var response = await _httpClient.PostAsJsonAsync(_apiBaseUrl + "Create", barco);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Barco>() ?? barco;
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

