using API.Models;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;

namespace Sistema_de_Gestion_de_Importaciones.Services;

public class BarcoService : IBarcoService
{
    private readonly HttpClient _httpClient;

    private readonly string _apiUrl;

    public BarcoService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiUrl = configuration["ApiSettings:BaseUrl"] + "/api/Barco";
    }

    public async Task<IEnumerable<Barco>> GetAllAsync()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<IEnumerable<Barco>>(_apiUrl);
            return result ?? Enumerable.Empty<Barco>();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Error al obtener los barcos: {ex.Message}", ex);
        }
    }

    public async Task<Barco> GetByIdAsync(int id)
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

