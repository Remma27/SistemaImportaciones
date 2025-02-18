using SistemaDeGestionDeImportaciones.Models;
using SistemaDeGestionDeImportaciones.Services.Interfaces;

namespace SistemaDeGestionDeImportaciones.Services
{

    public class EmpresaService : IEmpresaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;

        public EmpresaService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiUrl = configuration["ApiSettings:BaseUrl"] + "/api/Empresa";
        }

        public async Task<IEnumerable<Empresa>> GetAllAsync()
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<IEnumerable<Empresa>>(_apiUrl);
                return result ?? Enumerable.Empty<Empresa>();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al obtener las empresas: {ex.Message}", ex);
            }
        }

        public async Task<Empresa> GetByIdAsync(int id)
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<Empresa>($"{_apiUrl}/{id}");
                return result ?? throw new KeyNotFoundException($"No se encontró la empresa con ID {id}");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al obtener la empresa {id}: {ex.Message}", ex);
            }
        }

        public async Task<Empresa> CreateAsync(Empresa empresa)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(_apiUrl, empresa);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<Empresa>();
                return result ?? throw new InvalidOperationException("Error al crear la empresa");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al crear la empresa: {ex.Message}", ex);
            }
        }

        public async Task<Empresa> UpdateAsync(int id, Empresa empresa)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{_apiUrl}/{id}", empresa);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<Empresa>();
                return result ?? throw new InvalidOperationException("Error al actualizar la empresa");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al actualizar la empresa: {ex.Message}", ex);
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
                throw new Exception($"Error al eliminar la empresa: {ex.Message}", ex);
            }
        }
    }
}