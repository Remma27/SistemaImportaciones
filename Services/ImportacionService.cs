using API.Models;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;

namespace Sistema_de_Gestion_de_Importaciones.Services;

public class ImportacionService : IImportacionService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;

    public ImportacionService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiUrl = configuration["ApiSettings:BaseUrl"] + "/api/Importaciones";
    }

    public async Task<IEnumerable<Importacion>> GetAllAsync()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<IEnumerable<Importacion>>(_apiUrl);
            return result ?? Enumerable.Empty<Importacion>();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Error al obtener las importaciones: {ex.Message}", ex);
        }
    }

    public async Task<Importacion> GetByIdAsync(int id)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<Importacion>($"{_apiUrl}/{id}");
            return result ?? throw new KeyNotFoundException($"No se encontró la importación con ID {id}");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Error al obtener la importación {id}: {ex.Message}", ex);
        }
    }

    public async Task<Importacion> CreateAsync(Importacion importacion)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(_apiUrl, importacion);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<Importacion>();
            return result ?? throw new InvalidOperationException("Error al crear la importación");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Error al crear la importación: {ex.Message}", ex);
        }
    }

    public async Task<Importacion> UpdateAsync(int id, Importacion importacion)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{_apiUrl}/{id}", importacion);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<Importacion>();
            return result ?? throw new InvalidOperationException($"Error al actualizar la importación {id}");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Error al actualizar la importación {id}: {ex.Message}", ex);
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
            throw new Exception($"Error al eliminar la importación {id}: {ex.Message}", ex);
        }
    }
}
