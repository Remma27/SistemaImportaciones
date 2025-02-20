using Microsoft.AspNetCore.Mvc.Rendering;
using Sistema_de_Gestion_de_Importaciones.ViewModels;
using API.Models;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using System.Text.Json;

namespace SistemaDeGestionDeImportaciones.Services
{
    public class RegistroRequerimientosService : IRegistroRequerimientosService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;

        public RegistroRequerimientosService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiUrl = "api/Movimientos";
        }

        public async Task<Movimiento> RegistroRequerimientos(int id, Movimiento movimiento)
        {
            try
            {

                var url = $"{_apiUrl}/RegistroRequerimientos/{id}";
                var response = await _httpClient.PostAsJsonAsync(url, movimiento);
                var content = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Error al registrar requerimientos: {response.StatusCode}, {content}");
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                using JsonDocument document = JsonDocument.Parse(content);
                var root = document.RootElement;
                if (root.TryGetProperty("value", out JsonElement valueElement))
                {
                    var movimientoJson = valueElement.GetRawText();
                    var result = JsonSerializer.Deserialize<Movimiento>(movimientoJson, options);
                    return result ?? throw new Exception("Error al registrar requerimientos: respuesta nula");
                }
                return new Movimiento();
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
                var barcoUrl = "/api/Barco/GetAll";
                var response = await _httpClient.GetAsync(barcoUrl);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // Parse the JSON response structure
                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;

                // Check if the response has a 'value' property (common in ASP.NET Core API responses)
                var barcosArray = root.TryGetProperty("value", out var valueElement)
                    ? valueElement
                    : root;

                var barcos = JsonSerializer.Deserialize<List<Barco>>(barcosArray.GetRawText(), options);

                return barcos?.Select(b => new SelectListItem
                {
                    Value = b.id.ToString(),
                    Text = b.nombrebarco ?? string.Empty
                }) ?? Enumerable.Empty<SelectListItem>();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al obtener la lista de barcos: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                throw new Exception($"Error al deserializar la respuesta: {ex.Message}", ex);
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
                // Add logging to debug the API call
                Console.WriteLine($"Calling API: {_apiUrl}/RegistroRequerimientos?selectedBarco={barcoId}");

                var response = await _httpClient.GetAsync($"{_apiUrl}/RegistroRequerimientos?selectedBarco={barcoId}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Response: {content}"); // Debug log

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;

                if (root.TryGetProperty("data", out var dataElement))
                {
                    var registros = JsonSerializer.Deserialize<List<RegistroRequerimientosViewModel>>(
                        dataElement.GetRawText(),
                        options
                    );

                    // Log the number of records deserialized
                    Console.WriteLine($"Deserialized {registros?.Count ?? 0} records");

                    return registros ?? new List<RegistroRequerimientosViewModel>();
                }

                throw new Exception($"Estructura de respuesta inválida. Contenido: {content}");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al obtener registros: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                throw new Exception($"Error al deserializar la respuesta: {ex.Message}", ex);
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

