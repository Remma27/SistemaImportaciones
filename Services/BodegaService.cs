using API.Models;
using System.Text.Json;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;

namespace Sistema_de_Gestion_de_Importaciones.Services;

public class BodegaService : IBodegaService
{

    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;

    public BodegaService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        // Add /GetAll to match the API controller action
        _apiUrl = "api/Bodega/";
    }

    public async Task<IEnumerable<Empresa_Bodegas>> GetAllAsync()
    {
        try
        {
            var url = _apiUrl + "GetAll";
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
            var result = await _httpClient.GetFromJsonAsync<Empresa_Bodegas>($"{_apiUrl}/{id}");
            return result ?? throw new KeyNotFoundException($"No se encontró la bodega con ID {id}");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Error al obtener la bodega {id}: {ex.Message}", ex);
        }
    }

    public async Task<Empresa_Bodegas> CreateAsync(Empresa_Bodegas bodega)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(_apiUrl, bodega);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<Empresa_Bodegas>();
            return result ?? throw new InvalidOperationException("Error al crear la bodega");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Error al crear la bodega: {ex.Message}", ex);
        }
    }

    public async Task<Empresa_Bodegas> UpdateAsync(int id, Empresa_Bodegas bodega)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{_apiUrl}/{id}", bodega);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<Empresa_Bodegas>();
            return result ?? throw new InvalidOperationException("Error al actualizar la bodega");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Error al actualizar la bodega: {ex.Message}", ex);
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
            throw new Exception($"Error al eliminar la bodega: {ex.Message}", ex);
        }
    }
}