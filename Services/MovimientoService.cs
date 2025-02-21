using System.Text.Json;
using API.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sistema_de_Gestion_de_Importaciones.Models.ViewModels;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using Sistema_de_Gestion_de_Importaciones.ViewModels;

namespace Sistema_de_Gestion_de_Importaciones.Services
{
    public class MovimientoService : IMovimientoService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;
        private readonly ILogger<MovimientoService> _logger;

        public MovimientoService(HttpClient httpClient, IConfiguration configuration, ILogger<MovimientoService> logger)
        {
            _httpClient = httpClient;
            _apiUrl = configuration["ApiSettings:BaseUrl"] + "/api/Movimientos";
            _logger = logger;
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
                // Agregar "/Create" a la URL para llegar al endpoint correcto en la API
                var response = await _httpClient.PostAsJsonAsync($"{_apiUrl}/Create", movimiento);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<Movimiento>();
                return result ?? throw new InvalidOperationException("Error al crear el movimiento: respuesta nula");
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

        //Registro de Requerimientos
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

        public async Task<IEnumerable<SelectListItem>> GetImportacionesSelectListAsync()
        {
            try
            {
                _logger.LogInformation("Iniciando solicitud para obtener importaciones");

                // Corregir la URL para usar el endpoint correcto
                var response = await _httpClient.GetAsync($"{_apiUrl}/GetAll");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error HTTP {response.StatusCode}: {error}");
                    throw new HttpRequestException($"Error al obtener importaciones: {response.StatusCode}");
                }

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;

                if (root.TryGetProperty("value", out var valueElement))
                {
                    var importaciones = JsonSerializer.Deserialize<List<Importacion>>(valueElement.GetRawText(), options);
                    return importaciones?.Select(i => new SelectListItem
                    {
                        Value = i.id.ToString(),
                        Text = $"{i.Barco?.nombrebarco} - {i.fechahora:dd/MM/yyyy}"
                    }) ?? Enumerable.Empty<SelectListItem>();
                }

                return Enumerable.Empty<SelectListItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la lista de importaciones");
                throw new Exception($"Error al obtener la lista de importaciones: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<SelectListItem>> GetEmpresasSelectListAsync()
        {
            try
            {
                // Change the endpoint to match your API controller
                var empresaUrl = "/api/Empresa/GetAll";
                var response = await _httpClient.GetAsync(empresaUrl);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;

                // Check if the response has a 'value' property
                var empresasArray = root.TryGetProperty("value", out var valueElement)
                    ? valueElement
                    : root;

                var empresas = JsonSerializer.Deserialize<List<Empresa>>(empresasArray.GetRawText(), options);

                return empresas?.Select(e => new SelectListItem
                {
                    Value = e.id_empresa.ToString(),
                    Text = e.nombreempresa ?? string.Empty
                }) ?? Enumerable.Empty<SelectListItem>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error al obtener la lista de empresas");
                throw new Exception($"Error al obtener la lista de empresas: {ex.Message}", ex);
            }
        }

        async Task<List<RegistroRequerimientosViewModel>> IMovimientoService.GetRegistroRequerimientosAsync(int barcoId)
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

        async Task<RegistroRequerimientosViewModel> IMovimientoService.GetRegistroRequerimientoByIdAsync(int id)
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

        public async Task<List<InformeGeneralViewModel>> GetInformeGeneralAsync(int barcoId)
        {
            try
            {
                // Changed importacionId to barcoId to match the endpoint parameter
                var response = await _httpClient.GetAsync($"{_apiUrl}/InformeGeneral?importacionId={barcoId}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;

                if (root.TryGetProperty("data", out var dataElement))
                {
                    var informes = JsonSerializer.Deserialize<List<InformeGeneralViewModel>>(
                        dataElement.GetRawText(),
                        options
                    );

                    return informes ?? new List<InformeGeneralViewModel>();
                }

                throw new Exception($"Estructura de respuesta inválida. Contenido: {content}");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IEnumerable<Movimiento>> GetAllMovimientosAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_apiUrl}/GetAll");
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<IEnumerable<Movimiento>>();
                return result ?? Enumerable.Empty<Movimiento>();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al obtener todos los movimientos: {ex.Message}", ex);
            }
        }

        public async Task<Movimiento> GetMovimientoByIdAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_apiUrl}/{id}");
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<Movimiento>();
                return result ?? throw new KeyNotFoundException($"No se encontró el movimiento con ID {id}");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al obtener el movimiento {id}: {ex.Message}", ex);
            }
        }

        public async Task<Movimiento> CreateMovimientoAsync(Movimiento movimiento)
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

        public async Task UpdateMovimientoAsync(Movimiento movimiento)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{_apiUrl}/{movimiento.id}", movimiento);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al actualizar el movimiento {movimiento.id}: {ex.Message}", ex);
            }
        }

        public async Task DeleteMovimientoAsync(int id)
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

        public async Task UpdateAsync(int id, RegistroRequerimientosViewModel viewModel, string userId)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{_apiUrl}/{id}?userId={userId}", viewModel);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al actualizar el registro {id}: {ex.Message}", ex);
            }
        }
    }
}