using System.Text.Json;
using API.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
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
        private readonly IMemoryCache _memoryCache;
        private IConfiguration configuration;

        public MovimientoService(HttpClient httpClient, IConfiguration configuration, ILogger<MovimientoService> logger, IMemoryCache memoryCache)
        {
            _httpClient = httpClient;
            this.configuration = configuration;
            _apiBaseUrl = configuration["ApiSettings:BaseUrl"] + "/api/Movimientos";
            _logger = logger;
            _memoryCache = memoryCache;
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
                    return importaciones?
                        .OrderByDescending(i => i.fechahora)
                        .Select(i => new SelectListItem
                        {
                            Value = i.id.ToString(),
                            Text = $"{i.id} - {i.fechahora:dd/MM/yyyy} - {i.Barco?.nombrebarco ?? "Sin barco"}"
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

                return empresas?
                    .Where(e => e.estatus == 1)
                    .OrderBy(e => e.nombreempresa)
                    .Select(e => new SelectListItem
                    {
                        Value = e.id_empresa.ToString(),
                        Text = $"{e.id_empresa} - {e.nombreempresa ?? string.Empty}"
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
                Console.WriteLine($"API Response: {content}");

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
                _logger.LogInformation($"Solicitando registro requerimiento con ID: {id}");
                var url = $"{_apiBaseUrl}/Get?id={id}";
                _logger.LogInformation($"URL de la solicitud: {url}");

                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Respuesta de la API: {content}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = TryGetErrorMessage(content);
                    _logger.LogError($"Error al obtener el registro requerimiento {id}: {errorMessage}");
                    throw new HttpRequestException($"Error al obtener el registro requerimiento {id}: {response.StatusCode}, {content}");
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

                if (movimiento == null)
                    throw new InvalidOperationException($"No se encontró el movimiento con ID {id}");

                // Depurar los valores de IDs para diagnosticar el problema
                _logger.LogInformation($"Movimiento ID: {movimiento.id}, ImportacionID: {movimiento.idimportacion}, EmpresaID: {movimiento.idempresa}");

                // Obtener el nombre de la importación (barco) directamente desde la API
                string importacionNombre = "Desconocido";
                if (movimiento.idimportacion > 0)
                {
                    try
                    {
                        var importacionResponse = await _httpClient.GetAsync($"/api/Importaciones/Get?id={movimiento.idimportacion}");
                        if (importacionResponse.IsSuccessStatusCode)
                        {
                            var importacionContent = await importacionResponse.Content.ReadAsStringAsync();
                            _logger.LogInformation($"Respuesta API importación: {importacionContent}");

                            using var importacionDoc = JsonDocument.Parse(importacionContent);

                            if (importacionDoc.RootElement.TryGetProperty("value", out var impValueElement))
                            {
                                var importacion = JsonSerializer.Deserialize<Importacion>(impValueElement.GetRawText(), options);
                                if (importacion?.Barco != null)
                                {
                                    importacionNombre = $"{importacion.Barco.nombrebarco} - {importacion.fechahora:dd/MM/yyyy}";
                                    _logger.LogInformation($"Nombre importación encontrado: {importacionNombre}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al obtener detalles de la importación");
                    }
                }

                // Obtener el nombre de la empresa directamente desde la API
                string empresaNombre = "Desconocida";
                if (movimiento.idempresa > 0)
                {
                    try
                    {
                        var empresaResponse = await _httpClient.GetAsync($"/api/Empresa/Get?id={movimiento.idempresa}");
                        if (empresaResponse.IsSuccessStatusCode)
                        {
                            var empresaContent = await empresaResponse.Content.ReadAsStringAsync();
                            _logger.LogInformation($"Respuesta API empresa: {empresaContent}");

                            using var empresaDoc = JsonDocument.Parse(empresaContent);

                            if (empresaDoc.RootElement.TryGetProperty("value", out var empValueElement))
                            {
                                var empresa = JsonSerializer.Deserialize<Empresa>(empValueElement.GetRawText(), options);
                                if (empresa != null && !string.IsNullOrEmpty(empresa.nombreempresa))
                                {
                                    empresaNombre = empresa.nombreempresa;
                                    _logger.LogInformation($"Nombre empresa encontrado: {empresaNombre}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al obtener detalles de la empresa");
                    }
                }

                // Convertir el Movimiento a RegistroRequerimientosViewModel
                var viewModel = new RegistroRequerimientosViewModel
                {
                    IdMovimiento = movimiento.id,
                    FechaHora = movimiento.fechahora,
                    IdImportacion = movimiento.idimportacion,
                    IdEmpresa = movimiento.idempresa,
                    TipoTransaccion = movimiento.tipotransaccion,
                    CantidadRequerida = movimiento.cantidadrequerida,
                    CantidadCamiones = movimiento.cantidadcamiones,
                    Importacion = importacionNombre,
                    Empresa = empresaNombre
                };

                return viewModel;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"Error de HTTP al obtener el registro requerimiento {id}");
                throw new Exception($"Error al obtener el registro: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error general al obtener el registro requerimiento {id}");
                throw new Exception($"Error al procesar el registro: {ex.Message}", ex);
            }
        }

        public async Task<List<InformeGeneralViewModel>> GetInformeGeneralAsync(int barcoId)
        {
            try
            {
                string cacheKey = $"InformeGeneral_{barcoId}";

                if (_memoryCache.TryGetValue(cacheKey, out List<InformeGeneralViewModel>? cachedResult) && cachedResult != null)
                {
                    return cachedResult;
                }

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/InformeGeneral?importacionId={barcoId}&fields=empresaId,empresa,requeridoKg,requeridoTon,descargaKg,faltanteKg,tonFaltantes,conteoPlacas,porcentajeDescarga,camionesFaltantes");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;

                if (root.TryGetProperty("totalMovimientos", out var totalMovimientosElement))
                {

                    TotalMovimientos = totalMovimientosElement.GetInt32();
                }

                if (root.TryGetProperty("data", out var dataElement))
                {
                    var informes = JsonSerializer.Deserialize<List<InformeGeneralViewModel>>(
                        dataElement.GetRawText(),
                        options
                    );

                    if (informes != null)
                    {
                        _memoryCache.Set(cacheKey, informes, TimeSpan.FromMinutes(5));
                    }

                    return informes ?? new List<InformeGeneralViewModel>();
                }

                throw new Exception($"Estructura de respuesta inválida");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo informe general para barco {BarcoId}", barcoId);
                throw;
            }
        }

        public int TotalMovimientos { get; private set; }

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
                _logger.LogInformation($"Actualizando registro requerimiento {id}...");

                // Convert the view model to a Movimiento object
                var movimiento = new Movimiento
                {
                    id = viewModel.IdMovimiento,
                    idimportacion = viewModel.IdImportacion ?? 0,
                    idempresa = viewModel.IdEmpresa,
                    tipotransaccion = viewModel.TipoTransaccion ?? 1,
                    cantidadrequerida = viewModel.CantidadRequerida,
                    cantidadcamiones = viewModel.CantidadCamiones,
                    fechahora = viewModel.FechaHora ?? DateTime.Now,
                    idusuario = int.TryParse(userId, out int parsedId) ? parsedId : null
                };

                // Use the Edit endpoint with PUT method, as defined in the API controller
                var url = $"{_apiBaseUrl}/Edit";
                _logger.LogInformation($"Calling API endpoint: {url}");

                var response = await _httpClient.PutAsJsonAsync(url, movimiento);

                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"API error: {response.StatusCode}, Content: {content}");
                    throw new HttpRequestException($"Response status code does not indicate success: {(int)response.StatusCode} ({response.StatusCode})");
                }

                _logger.LogInformation($"Registro requerimiento {id} actualizado con éxito");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"Error al actualizar el registro {id}: {ex.Message}");
                throw new Exception($"Error al actualizar el registro {id}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error inesperado al actualizar el registro {id}: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Movimiento>> CalculoMovimientos(int importacionId, int idempresa)
        {
            try
            {
                string cacheKey = $"CalculoMovimientos_{importacionId}_{idempresa}";

                if (_memoryCache.TryGetValue(cacheKey, out List<Movimiento>? cachedResult) && cachedResult != null)
                {
                    return cachedResult;
                }

                _logger.LogDebug($"Calculando movimientos: importación {importacionId}, empresa {idempresa}");

                var url = $"{_apiBaseUrl}/CalculoMovimientos?importacionId={importacionId}&idempresa={idempresa}";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Error API ({response.StatusCode}): {url}");
                    return new List<Movimiento>();
                }

                var apiResponse = await response.Content.ReadFromJsonAsync<MovimientosApiResponse>();

                if (apiResponse?.Data == null || !apiResponse.Data.Any())
                {
                    return new List<Movimiento>();
                }

                var result = apiResponse.Data.Select(dto => new Movimiento
                {
                    id = dto.id,
                    bodega = string.IsNullOrEmpty(dto.bodega) ? null :
                            int.TryParse(dto.bodega, out int bod) ? bod : null,
                    guia = string.IsNullOrEmpty(dto.guia) ? null :
                          int.TryParse(dto.guia, out int gui) ? gui : null,
                    guia_alterna = dto.guia_alterna,
                    placa = dto.placa,
                    placa_alterna = dto.placa_alterna,
                    cantidadentregada = dto.cantidadentregada,
                    cantidadrequerida = dto.cantidadrequerida,
                    peso_faltante = dto.peso_faltante,
                    porcentaje = dto.porcentaje,
                    escotilla = dto.escotilla,
                }).ToList();

                _memoryCache.Set(cacheKey, result, TimeSpan.FromMinutes(3));

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular los movimientos");
                return new List<Movimiento>();
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
                _logger.LogInformation($"Solicitando datos de escotillas para importación {importacionId}");
                var url = $"{_apiBaseUrl}/CalculoEscotillas?importacionId={importacionId}";
                _logger.LogDebug($"URL de solicitud: {url}");

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Error al obtener escotillas: Status {response.StatusCode}");
                    return new EscotillasResumenViewModel();
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug($"Respuesta API escotillas: {content.Substring(0, Math.Min(500, content.Length))}...");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var apiResponse = JsonSerializer.Deserialize<EscotillaApiResponse>(content, options);

                _logger.LogInformation($"Escotillas recibidas: {apiResponse?.Escotillas?.Count ?? 0}");

                return new EscotillasResumenViewModel
                {
                    Escotillas = apiResponse?.Escotillas?.Select(e => new EscotillaViewModel
                    {
                        NumeroEscotilla = e.NumeroEscotilla,
                        CapacidadKg = e.CapacidadKg,
                        DescargaRealKg = e.DescargaRealKg,
                        DiferenciaKg = e.DiferenciaKg,
                        Porcentaje = e.Porcentaje,
                        Estado = e.Estado
                    }).ToList() ?? new List<EscotillaViewModel>(),
                    CapacidadTotal = apiResponse?.Totales?.CapacidadTotal ?? 0,
                    DescargaTotal = apiResponse?.Totales?.DescargaTotal ?? 0,
                    DiferenciaTotal = apiResponse?.Totales?.DiferenciaTotal ?? 0,
                    PorcentajeTotal = apiResponse?.Totales?.PorcentajeTotal ?? 0,
                    TotalKilosRequeridos = apiResponse?.Totales?.TotalKilosRequeridos ?? 0,
                    EstadoGeneral = apiResponse?.Totales?.EstadoGeneral ?? "Sin información"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener datos de escotillas");
                return new EscotillasResumenViewModel();
            }
        }

        public async Task<List<RegistroPesajesIndividual>> GetAllMovimientosByImportacionAsync(int importacionId)
        {
            try
            {
                _logger.LogInformation($"Solicitando movimientos para importación {importacionId}");

                var url = $"{_apiBaseUrl}/GetAllByImportacion?importacionId={importacionId}";

                _logger.LogInformation($"URL API: {url}");

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Respuesta recibida de la API. Longitud: {content.Length} caracteres");

                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;

                if (root.TryGetProperty("data", out var dataElement))
                {
                    if (dataElement.ValueKind == JsonValueKind.Array)
                    {
                        var resultado = new List<RegistroPesajesIndividual>();

                        foreach (var element in dataElement.EnumerateArray())
                        {
                            try
                            {
                                var item = new RegistroPesajesIndividual
                                {
                                    Id = GetIntValue(element, "id"),
                                    FechaHora = GetDateTimeValue(element, "fechaHora", DateTime.Now),
                                    IdImportacion = importacionId,
                                    IdEmpresa = GetIntValue(element, "idEmpresa"),
                                    EmpresaNombre = GetStringValue(element, "empresaNombre") ??
                                                    GetStringValue(element, "empresa") ??
                                                    "Sin Empresa",
                                    Escotilla = GetIntValue(element, "escotilla"),
                                    Bodega = GetStringValue(element, "bodega"),

                                    Guia = element.TryGetProperty("guia", out var guia) && guia.ValueKind != JsonValueKind.Null
                                        ? guia.ValueKind == JsonValueKind.Number
                                            ? guia.GetInt32().ToString()
                                            : guia.GetString() ?? ""
                                        : "",

                                    GuiaAlterna = GetStringValue(element, "guiaAlterna"),
                                    Placa = GetStringValue(element, "placa"),
                                    PlacaAlterna = GetStringValue(element, "placaAlterna"),
                                    PesoEntregado = GetDecimalValue(element, "pesoEntregadoKg"),
                                    PesoRequerido = GetDecimalValue(element, "cantidadRetirarKg"),
                                    CantidadRetiradaKg = GetDecimalValue(element, "cantidadRetiradaKg"),
                                    PesoFaltante = GetDecimalValue(element, "pesoFaltante"),
                                    CantidadRequeridaQuintales = GetDecimalValue(element, "cantidadRequeridaQuintales"),
                                    CantidadEntregadaQuintales = GetDecimalValue(element, "cantidadEntregadaQuintales"),
                                    CantidadRequeridaLibras = GetDecimalValue(element, "cantidadRequeridaLibras"),
                                    CantidadEntregadaLibras = GetDecimalValue(element, "cantidadEntregadaLibras"),
                                    TipoTransaccion = GetIntValue(element, "tipoTransaccion")
                                };

                                resultado.Add(item);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error al procesar un elemento del array JSON");
                            }
                        }

                        _logger.LogInformation($"Movimientos procesados exitosamente: {resultado.Count}");
                        return resultado;
                    }
                }

                _logger.LogWarning("La respuesta de la API no contiene un array de datos válido");
                return new List<RegistroPesajesIndividual>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los movimientos de la importación {ImportacionId}", importacionId);
                throw;
            }
        }

        private int GetIntValue(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop) &&
                prop.ValueKind != JsonValueKind.Null)
            {
                return prop.TryGetInt32(out var value) ? value : 0;
            }
            return 0;
        }

        private decimal GetDecimalValue(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop) &&
                prop.ValueKind != JsonValueKind.Null)
            {
                return prop.TryGetDecimal(out var value) ? value : 0;
            }
            return 0;
        }

        private string GetStringValue(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Null)
                    return "";

                if (prop.ValueKind == JsonValueKind.String)
                    return prop.GetString() ?? "";

                return prop.ToString();
            }
            return "";
        }

        private DateTime GetDateTimeValue(JsonElement element, string propertyName, DateTime defaultValue)
        {
            if (element.TryGetProperty(propertyName, out var prop) &&
                prop.ValueKind != JsonValueKind.Null)
            {
                return prop.TryGetDateTime(out var value) ? value : defaultValue;
            }
            return defaultValue;
        }

        public async Task<ReporteEscotillasPorEmpresaViewModel> GetEscotillasPorEmpresaAsync(int importacionId)
        {
            try
            {
                var url = $"{_apiBaseUrl}/CalculoEscotillasPorEmpresa?importacionId={importacionId}";
                _logger.LogInformation($"Solicitando datos de escotillas por empresa: {url}");

                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Error al obtener escotillas por empresa: {response.StatusCode}, {content}");
                    throw new HttpRequestException($"API error: {response.StatusCode}");
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                using JsonDocument document = JsonDocument.Parse(content);
                var root = document.RootElement;

                var viewModel = new ReporteEscotillasPorEmpresaViewModel();

                if (root.TryGetProperty("empresas", out JsonElement empresasElement))
                {
                    viewModel.Empresas = JsonSerializer.Deserialize<List<EmpresaEscotillasViewModel>>(
                        empresasElement.GetRawText(), options) ?? new List<EmpresaEscotillasViewModel>();
                }

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener datos de escotillas por empresa");
                throw;
            }
        }
    }
}