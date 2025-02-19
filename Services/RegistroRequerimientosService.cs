using Microsoft.AspNetCore.Mvc.Rendering;
using Sistema_de_Gestion_de_Importaciones.ViewModels;
using API.Models;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;

namespace SistemaDeGestionDeImportaciones.Services
{
    public class RegistroRequerimientosService : IRegistroRequerimientosService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;

        public RegistroRequerimientosService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiUrl = configuration["ApiSettings:BaseUrl"] + "/api/Movimientos";
        }

        public async Task<Movimiento> RegistroRequerimientos(int id, Movimiento movimiento)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_apiUrl}/registro-requerimientos/{id}", movimiento);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<Movimiento>();
                return result ?? throw new Exception("Error al registrar requerimientos: respuesta nula");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al registrar requerimientos: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<SelectListItem>> GetBarcosSelectListAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<IEnumerable<SelectListItem>>($"{_apiUrl}/barcos");
                return response ?? Enumerable.Empty<SelectListItem>();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al obtener la lista de barcos: {ex.Message}", ex);
            }
        }

        public async Task<string?> GetRegistroRequerimientosAsync(int id)
        {
            // Sample implementation
            var response = await _httpClient.GetStringAsync($"{_apiUrl}/registro/{id}");
            return response;
        }

        public async Task<IEnumerable<SelectListItem>> GetImportacionesSelectListAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<IEnumerable<SelectListItem>>($"{_apiUrl}/importaciones");
                return response ?? Enumerable.Empty<SelectListItem>();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al obtener la lista de importaciones: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<SelectListItem>> GetEmpresasSelectListAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<IEnumerable<SelectListItem>>($"{_apiUrl}/empresas");
                return response ?? Enumerable.Empty<SelectListItem>();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al obtener la lista de empresas: {ex.Message}", ex);
            }
        }

        public async Task CreateAsync(Movimiento viewModel)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_apiUrl}/registro-requerimientos", viewModel);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al crear el registro de requerimientos: {ex.Message}", ex);
            }
        }

        public async Task<Movimiento?> GetRegistroRequerimientoByIdAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<Movimiento>($"{_apiUrl}/registro-requerimientos/{id}");
                return response ?? throw new Exception("Error al obtener el registro de requerimientos: respuesta nula");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al obtener el registro de requerimientos: {ex.Message}", ex);
            }
        }

        public async Task UpdateAsync(int id, Movimiento viewModel, int userId)
        {
            try
            {
                var movimiento = new Movimiento
                {
                    fechahora = viewModel.fechahora,
                    idimportacion = viewModel.idimportacion,
                    idempresa = viewModel.idempresa,
                    tipotransaccion = viewModel.tipotransaccion,
                    cantidadrequerida = (decimal?)viewModel.cantidadrequerida,
                    cantidadcamiones = viewModel.cantidadcamiones
                };
                var response = await _httpClient.PutAsJsonAsync($"{_apiUrl}/registro-requerimientos/{id}", movimiento);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al actualizar el registro de requerimientos: {ex.Message}", ex);
            }
        }

        async Task<List<RegistroRequerimientosViewModel>> IRegistroRequerimientosService.GetRegistroRequerimientosAsync(int barcoId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<RegistroRequerimientosViewModel>>($"{_apiUrl}/by-barco/{barcoId}");
                return response ?? new List<RegistroRequerimientosViewModel>();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al obtener registros: {ex.Message}", ex);
            }
        }

        async Task<RegistroRequerimientosViewModel> IRegistroRequerimientosService.GetRegistroRequerimientoByIdAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<RegistroRequerimientosViewModel>($"{_apiUrl}/{id}");
                return response ?? throw new Exception($"No se encontró el registro con ID {id}");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al obtener el registro: {ex.Message}", ex);
            }
        }

        public async Task UpdateAsync(int id, RegistroRequerimientosViewModel viewModel, string userId)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{_apiUrl}/{id}", viewModel);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al actualizar el registro: {ex.Message}", ex);
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
                throw new Exception($"Error al eliminar el registro: {ex.Message}", ex);
            }
        }
    }
}

