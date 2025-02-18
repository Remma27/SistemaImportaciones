using SistemaDeGestionDeImportaciones.Models;
using SistemaDeGestionDeImportaciones.Services.Interfaces;

namespace SistemaDeGestionDeImportaciones.Services;

public class UsuarioService : IUsuarioService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;

    public UsuarioService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiUrl = configuration["ApiSettings:BaseUrl"] + "/api/Usuario";
    }

    public async Task<IEnumerable<Usuario>> GetAllAsync()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<IEnumerable<Usuario>>(_apiUrl);
            return result ?? Enumerable.Empty<Usuario>();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Error al obtener los usuarios: {ex.Message}", ex);
        }
    }

    public async Task<Usuario> GetByIdAsync(int id)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<Usuario>($"{_apiUrl}/{id}");
            return result ?? throw new KeyNotFoundException($"No se encontró el usuario con ID {id}");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Error al obtener el usuario {id}: {ex.Message}", ex);
        }
    }

    public async Task<Usuario> CreateAsync(Usuario usuario)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(_apiUrl, usuario);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<Usuario>();
            return result ?? throw new InvalidOperationException("Error al crear el usuario");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Error al crear el usuario: {ex.Message}", ex);
        }
    }

    public async Task<Usuario> UpdateAsync(int id, Usuario usuario)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{_apiUrl}/{id}", usuario);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<Usuario>();
            return result ?? throw new InvalidOperationException("Error al actualizar el usuario");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Error al actualizar el usuario: {ex.Message}", ex);
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
            throw new Exception($"Error al eliminar el usuario: {ex.Message}", ex);
        }
    }


}