using API.Models;
using System.Text.Json;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;

namespace Sistema_de_Gestion_de_Importaciones.Services;

public class BarcoService : IBarcoService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;
    private readonly ILogger<BarcoService> _logger;

    public BarcoService(HttpClient httpClient, IConfiguration configuration, ILogger<BarcoService> logger)
    {
        _httpClient = httpClient;
        _apiUrl = "api/Barco/";
        _logger = logger;
    }

    public async Task<IEnumerable<Barco>> GetAllAsync()
    {
        try
        {
            var url = _apiUrl + "GetAll";
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
            var result = await _httpClient.GetFromJsonAsync<Barco>($"{_apiUrl}/{id}");
            return result ?? throw new KeyNotFoundException($"No se encontró el barco con ID {id}");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Error al obtener el barco {id}: {ex.Message}", ex);
        }
    }

    public async Task<Barco> CreateAsync(Barco barco)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(_apiUrl, barco);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<Barco>();
            return result ?? throw new InvalidOperationException("Error al crear el barco");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Error al crear el barco: {ex.Message}", ex);
        }
    }

    public async Task<Barco> UpdateAsync(int id, Barco barco)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{_apiUrl}/{id}", barco);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<Barco>();
            return result ?? throw new InvalidOperationException("Error al actualizar el barco");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Error al actualizar el barco: {ex.Message}", ex);
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{_apiUrl}/{id}");
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Error al eliminar el barco: {ex.Message}", ex);
        }
    }
}

