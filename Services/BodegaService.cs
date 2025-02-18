using SistemaDeGestionDeImportaciones.Models;
using SistemaDeGestionDeImportaciones.Services.Interfaces;

namespace SistemaDeGestionDeImportaciones.Services;

public class BodegaService : IBodegaService
{

    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;

    public BodegaService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiUrl = configuration["ApiSettings:BaseUrl"] + "/api/Bodega";
    }

    public async Task<IEnumerable<Bodega>> GetAllAsync()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<IEnumerable<Bodega>>(_apiUrl);
            return result ?? Enumerable.Empty<Bodega>();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Error al obtener las bodegas: {ex.Message}", ex);
        }
    }

    public async Task<Bodega> GetByIdAsync(int id)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<Bodega>($"{_apiUrl}/{id}");
            return result ?? throw new KeyNotFoundException($"No se encontró la bodega con ID {id}");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Error al obtener la bodega {id}: {ex.Message}", ex);
        }
    }

    public async Task<Bodega> CreateAsync(Bodega bodega)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(_apiUrl, bodega);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<Bodega>();
            return result ?? throw new InvalidOperationException("Error al crear la bodega");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Error al crear la bodega: {ex.Message}", ex);
        }
    }

    public async Task<Bodega> UpdateAsync(int id, Bodega bodega)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{_apiUrl}/{id}", bodega);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<Bodega>();
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