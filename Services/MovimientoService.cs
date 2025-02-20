using API.Models;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;

namespace Sistema_de_Gestion_de_Importaciones.Services
{
    public class MovimientoService : IMovimientoService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;

        public MovimientoService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiUrl = configuration["ApiSettings:BaseUrl"] + "/api/Movimientos";
        }

        public async Task<IEnumerable<Movimiento>> GetAllAsync()
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<IEnumerable<Movimiento>>(_apiUrl);
                return result ?? Enumerable.Empty<Movimiento>();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al obtener los movimientos: {ex.Message}", ex);
            }
        }

        public async Task<Movimiento> GetByIdAsync(int id)
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<Movimiento>($"{_apiUrl}/{id}");
                return result ?? throw new KeyNotFoundException($"No se encontró el movimiento con ID {id}");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al obtener el movimiento {id}: {ex.Message}", ex);
            }
        }

        public async Task<Movimiento> CreateAsync(Movimiento movimiento)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(_apiUrl, movimiento);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<Movimiento>();
                return result ?? throw new InvalidOperationException("Error al crear el movimiento");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al crear el movimiento: {ex.Message}", ex);
            }
        }

        public async Task<Movimiento> UpdateAsync(int id, Movimiento movimiento)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{_apiUrl}/{id}", movimiento);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<Movimiento>();
                return result ?? throw new InvalidOperationException($"Error al actualizar el movimiento con ID {id}");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al actualizar el movimiento {id}: {ex.Message}", ex);
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
                throw new Exception($"Error al eliminar el movimiento {id}: {ex.Message}", ex);
            }
        }

        public async Task<Movimiento> InformeGeneral(int id, Movimiento movimiento)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_apiUrl}/InformeGeneral?importacionId={id}", movimiento);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<Movimiento>();
                return result ?? throw new Exception("Error al generar informe general: respuesta nula");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al generar informe general: {ex.Message}", ex);
            }
        }
    }
}