using API.Models;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;

namespace Sistema_de_Gestion_de_Importaciones.Services;

public class BodegaService : IBodegaService
{

    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;

    public BodegaService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiUrl = configuration["ApiSettings:BaseUrl"] + "/api/Bodega";
    }

    public async Task<IEnumerable<Empresa_Bodegas>> GetAllAsync()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<IEnumerable<Empresa_Bodegas>>(_apiUrl);
            return result ?? Enumerable.Empty<Empresa_Bodegas>();
        }
        catch (HttpRequestException ex)
        {
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