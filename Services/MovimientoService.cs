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
        private readonly string _apiBaseUrl;
        private readonly ILogger<MovimientoService> _logger;

        public MovimientoService(HttpClient httpClient, IConfiguration configuration, ILogger<MovimientoService> logger)
        {
            _httpClient = httpClient;
            _apiBaseUrl = configuration["ApiSettings:BaseUrl"] + "/api/Movimientos";
            _logger = logger;
        }

        public async Task<IEnumerable<Movimiento>> GetAllAsync()
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<IEnumerable<Movimiento>>(_apiBaseUrl);
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
                _logger.LogInformation($"Solicitando movimiento con ID: {id}");
                var url = $"{_apiBaseUrl}/Get?id={id}";
                _logger.LogInformation($"URL de la solicitud: {url}");

                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Respuesta de la API: {content}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = TryGetErrorMessage(content);
                    _logger.LogError($"Error al obtener movimiento {id}: {errorMessage}");
                    throw new HttpRequestException($"Error al obtener el movimiento {id}: {response.StatusCode}, {content}");
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                using JsonDocument document = JsonDocument.Parse(content);
                Movimiento? movimiento = null;

                if (document.RootElement.TryGetProperty("value", out JsonElement valueElement))
                {
                    var movimientoJson = valueElement.GetRawText();
                    movimiento = JsonSerializer.Deserialize<Movimiento>(movimientoJson, options);
                }
                else
                {
                    movimiento = JsonSerializer.Deserialize<Movimiento>(content, options);
                }

                return movimiento ?? throw new InvalidOperationException($"No se encontró el movimiento con ID {id}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"Error de HTTP al obtener el movimiento {id}");
                throw new Exception($"Error al obtener el movimiento {id}: {ex.Message}", ex);
            }
        }

        public async Task<Movimiento> CreateAsync(Movimiento movimiento)
        {
            try
            {
                _logger.LogInformation($"Enviando movimiento a API: {JsonSerializer.Serialize(movimiento)}");

                var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/Create", movimiento);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error API ({response.StatusCode}): {error}");
                    throw new HttpRequestException($"Error al crear movimiento: {response.StatusCode} - {error}");
                }

                var result = await response.Content.ReadFromJsonAsync<Movimiento>();
                return result ?? throw new InvalidOperationException("La API devolvió un resultado nulo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el movimiento");
                throw new Exception($"Error al crear el movimiento: {ex.Message}", ex);
            }
        }

        public async Task<Movimiento> UpdateAsync(int id, Movimiento movimiento)
        {
            try
            {
                _logger.LogInformation($"Actualizando movimiento {id}: {JsonSerializer.Serialize(movimiento)}");

                var url = $"{_apiBaseUrl}/Edit";
                var response = await _httpClient.PutAsJsonAsync(url, movimiento);

                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error al actualizar movimiento: {content}");
                    throw new HttpRequestException($"Error al actualizar: {response.StatusCode} - {content}");
                }

                var result = await response.Content.ReadFromJsonAsync<Movimiento>();
                return result ?? throw new InvalidOperationException($"Error al actualizar el movimiento {id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar movimiento {id}");
                throw new Exception($"Error al actualizar el movimiento {id}: {ex.Message}", ex);
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                _logger.LogInformation($"Iniciando eliminación del movimiento {id}");
                var url = $"{_apiBaseUrl}/Delete?id={id}";
                _logger.LogInformation($"URL de eliminación: {url}");

                var response = await _httpClient.DeleteAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Respuesta del servidor: Status={response.StatusCode}, Content={content}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Error al eliminar: Status={response.StatusCode}, Content={content}");
                    throw new HttpRequestException($"Error al eliminar el movimiento: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar el movimiento {id}");
                throw;
            }
        }

        public async Task<Movimiento> InformeGeneral(int id, Movimiento movimiento)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/InformeGeneral?importacionId={id}", movimiento);
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

                var url = $"{_apiBaseUrl}/RegistroRequerimientos/{id}";
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
                var response = await _httpClient.GetAsync("/api/Importaciones/GetAll");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"API Response: {content}");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;

                if (root.TryGetProperty("value", out var importacionesElement))
                {
                    var importaciones = JsonSerializer.Deserialize<List<Importacion>>(importacionesElement.GetRawText(), options);
                    return importaciones?.Select(i => new SelectListItem
                    {
                        Value = i.id.ToString(),
                        Text = $"ID: {i.id} - {i.Barco?.nombrebarco ?? "Sin barco"} - {i.fechahora:dd/MM/yyyy}"
                    }) ?? Enumerable.Empty<SelectListItem>();
                }

                return Enumerable.Empty<SelectListItem>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error al obtener la lista de importaciones");
                throw new Exception($"Error al obtener la lista de importaciones: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<SelectListItem>> GetImportacionesSelectListAsync()
        {
            try
            {
                _logger.LogInformation("Iniciando solicitud para obtener importaciones");

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/GetAll");

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
                Console.WriteLine($"Calling API: {_apiBaseUrl}/RegistroRequerimientos?selectedBarco={barcoId}");

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/RegistroRequerimientos?selectedBarco={barcoId}");
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
                var response = await _httpClient.GetFromJsonAsync<RegistroRequerimientosViewModel>($"{_apiBaseUrl}/{id}");
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
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/InformeGeneral?importacionId={barcoId}");
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
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/GetAll");
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
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/{id}");
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
                var response = await _httpClient.PostAsJsonAsync(_apiBaseUrl, movimiento);
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
                var response = await _httpClient.PutAsJsonAsync($"{_apiBaseUrl}/{movimiento.id}", movimiento);
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
                var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/{id}");
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
                var response = await _httpClient.PutAsJsonAsync($"{_apiBaseUrl}/{id}?userId={userId}", viewModel);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al actualizar el registro {id}: {ex.Message}", ex);
            }
        }
        public async Task<List<Movimiento>> CalculoMovimientos(int importacionId, int idempresa)
        {
            try
            {
                _logger.LogInformation($"Solicitando cálculo de movimientos para importación {importacionId} y empresa {idempresa}");

                var url = $"{_apiBaseUrl}/CalculoMovimientos?importacionId={importacionId}&idempresa={idempresa}";
                _logger.LogInformation($"URL de la solicitud: {url}");

                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Respuesta API: {content}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = TryGetErrorMessage(content);
                    throw new HttpRequestException($"Error del servidor: {errorMessage}");
                }

                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;

                if (root.TryGetProperty("data", out JsonElement dataElement))
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var movimientosDtos = JsonSerializer.Deserialize<List<MovimientosCumulatedDto>>(dataElement.GetRawText(), options);

                    if (movimientosDtos == null || !movimientosDtos.Any())
                    {
                        _logger.LogInformation("No se encontraron movimientos para procesar");
                        return new List<Movimiento>();
                    }

                    return movimientosDtos.Select(m => new Movimiento
                    {
                        id = m.id,
                        bodega = int.TryParse(m.bodega, out int bod) ? bod : null,
                        guia = int.TryParse(m.guia, out int gui) ? gui : null,
                        guia_alterna = m.guia_alterna,
                        placa = m.placa,
                        placa_alterna = m.placa_alterna,
                        cantidadrequerida = m.cantidadrequerida,
                        cantidadentregada = m.cantidadentregada,
                        peso_faltante = m.peso_faltante,
                        porcentaje = m.porcentaje
                    }).ToList();
                }

                return new List<Movimiento>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular los movimientos: {Message}", ex.Message);
                throw new Exception($"Error al calcular los movimientos: {ex.Message}", ex);
            }
        }

        public async Task<EscotillaApiResponse> GetEscotillasApiDataAsync(int importacionId)
        {
            try
            {
                var url = $"{_apiBaseUrl}/CalculoEscotillas?importacionId={importacionId}";
                var response = await _httpClient.GetAsync(url);

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<EscotillaApiResponse>(content, options)
                    ?? new EscotillaApiResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener datos de escotillas");
                throw;
            }
        }

        private string TryGetErrorMessage(string content)
        {
            try
            {
                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;

                if (root.TryGetProperty("message", out var messageElement))
                {
                    return messageElement.GetString() ?? "Error desconocido";
                }

                return content;
            }
            catch
            {
                return content;
            }
        }

        public async Task<EscotillasResumenViewModel> GetEscotillasDataAsync(int importacionId)
        {
            try
            {
                var url = $"{_apiBaseUrl}/CalculoEscotillas?importacionId={importacionId}";
                var response = await _httpClient.GetAsync(url);

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var apiResponse = JsonSerializer.Deserialize<EscotillaApiResponse>(content, options)
                    ?? new EscotillaApiResponse();

                return new EscotillasResumenViewModel
                {
                    Escotillas = apiResponse.Escotillas.Select(e => new EscotillaViewModel
                    {
                        NumeroEscotilla = e.NumeroEscotilla,
                        CapacidadKg = e.CapacidadKg,
                        DescargaRealKg = e.DescargaRealKg,
                        DiferenciaKg = e.DiferenciaKg,
                        Porcentaje = e.Porcentaje,
                        Estado = e.Estado
                    }).ToList(),
                    CapacidadTotal = apiResponse.Totales.CapacidadTotal,
                    DescargaTotal = apiResponse.Totales.DescargaTotal,
                    DiferenciaTotal = apiResponse.Totales.DiferenciaTotal,
                    PorcentajeTotal = apiResponse.Totales.PorcentajeTotal,
                    EstadoGeneral = apiResponse.Totales.EstadoGeneral
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener datos de escotillas");
                throw;
            }
        }
    }
}