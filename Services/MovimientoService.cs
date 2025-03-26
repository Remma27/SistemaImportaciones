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
                _logger.LogInformation("Iniciando solicitud GetAllAsync");
                var response = await _httpClient.GetAsync(_apiBaseUrl + "/GetAll");
                var content = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation($"Respuesta recibida: Status={response.StatusCode}");
                _logger.LogDebug($"Contenido de respuesta: {content.Substring(0, Math.Min(100, content.Length))}...");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Error del API: {response.StatusCode}, Content={content}");
                    throw new HttpRequestException($"API error: {response.StatusCode}");
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                using JsonDocument document = JsonDocument.Parse(content);
                var root = document.RootElement;
                
                _logger.LogDebug($"JSON Root Kind: {root.ValueKind}");
                
                if (root.ValueKind == JsonValueKind.Array)
                {
                    _logger.LogInformation("Detectado formato de array directo");
                    var movimientos = JsonSerializer.Deserialize<List<Movimiento>>(content, options);
                    _logger.LogInformation($"Movimientos deserializados: {movimientos?.Count ?? 0}");
                    return movimientos ?? new List<Movimiento>();
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("value", out JsonElement valueElement))
                    {
                        _logger.LogInformation("Detectado formato con propiedad 'value'");
                        var movimientosJson = valueElement.GetRawText();
                        var movimientos = JsonSerializer.Deserialize<List<Movimiento>>(movimientosJson, options);
                        return movimientos ?? new List<Movimiento>();
                    }
                    else
                    {
                        _logger.LogInformation("Intentando deserialización con Newtonsoft.Json");
                        try
                        {
                            var movimientos = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Movimiento>>(content);
                            if (movimientos != null)
                            {
                                _logger.LogInformation($"Movimientos deserializados con Newtonsoft: {movimientos.Count}");
                                return movimientos;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error con Newtonsoft.Json");
                        }
                    }
                }

                _logger.LogWarning("No se pudo deserializar la respuesta en ningún formato conocido");
                return new List<Movimiento>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error al obtener los movimientos");
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

                var root = document.RootElement;
                
                _logger.LogDebug($"JSON Root Kind: {root.ValueKind}");
                
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("value", out JsonElement valueElement))
                    {
                        var movimientoJson = valueElement.GetRawText();
                        movimiento = JsonSerializer.Deserialize<Movimiento>(movimientoJson, options);
                    }
                    else
                    {
                        movimiento = JsonSerializer.Deserialize<Movimiento>(content, options);
                    }
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
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        try
                        {
                            using (JsonDocument doc = JsonDocument.Parse(content))
                            {
                                if (doc.RootElement.TryGetProperty("message", out JsonElement messageElement))
                                {
                                    string mensaje = messageElement.GetString() ?? "No se puede eliminar el movimiento";
                                    
                                    if (mensaje.Contains("relacionado") || mensaje.Contains("asociado") || 
                                        mensaje.Contains("depende") || mensaje.Contains("utilizado"))
                                    {
                                        _logger.LogWarning($"Movimiento con ID={id} tiene dependencias: {mensaje}");
                                        throw new InvalidOperationException(mensaje);
                                    }
                                    
                                    throw new InvalidOperationException(mensaje);
                                }
                            }
                        }
                        catch (JsonException jsonEx)
                        {
                            _logger.LogError(jsonEx, $"Error al analizar la respuesta JSON: {content}");
                        }
                        
                        if (content.Contains("relacionado") || content.Contains("asociado") || 
                            content.Contains("depende") || content.Contains("utilizado"))
                        {
                            throw new InvalidOperationException("No se puede eliminar este movimiento porque está relacionado con otros registros.");
                        }
                    }
                    
                    throw new HttpRequestException($"Error al eliminar el movimiento: {response.StatusCode}, {content}");
                }
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar el movimiento {id}");
                throw new Exception($"Error al eliminar el movimiento: {ex.Message}", ex);
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
                _logger.LogInformation("Solicitando lista de barcos para importaciones");
                var response = await _httpClient.GetAsync("/api/Importaciones/GetAll");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Error del API: {response.StatusCode}");
                    throw new HttpRequestException($"API error: {response.StatusCode}");
                }
                
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"API Response recibida, longitud: {content.Length}");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;
                
                _logger.LogDebug($"JSON Root Kind: {root.ValueKind}");
                
                List<Importacion>? importaciones = null;
                
                if (root.ValueKind == JsonValueKind.Array)
                {
                    _logger.LogInformation("Detectado formato de array directo");
                    importaciones = JsonSerializer.Deserialize<List<Importacion>>(content, options);
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("value", out JsonElement valueElement))
                    {
                        _logger.LogInformation("Detectado formato con propiedad 'value'");
                        var importacionesJson = valueElement.GetRawText();
                        importaciones = JsonSerializer.Deserialize<List<Importacion>>(importacionesJson, options);
                    }
                }
                
                if (importaciones == null || !importaciones.Any())
                {
                    _logger.LogWarning("No se encontraron importaciones o no se pudo deserializar la respuesta");
                    return Enumerable.Empty<SelectListItem>();
                }
                
                _logger.LogInformation($"Importaciones deserializadas: {importaciones.Count}");
                
                return importaciones
                    .OrderByDescending(i => i.fechahora)
                    .Select(i => new SelectListItem
                    {
                        Value = i.id.ToString(),
                        Text = $"{i.id} - {i.fechahora:dd/MM/yyyy} - {i.Barco?.nombrebarco ?? "Sin barco"}"
                    }).ToList();
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
                
                var url = "/api/Importaciones/GetAll";
                _logger.LogDebug($"URL: {url}");
                
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error HTTP {response.StatusCode}: {error}");
                    throw new HttpRequestException($"Error al obtener importaciones: {response.StatusCode}");
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Respuesta recibida. Longitud: {content.Length} caracteres");
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;
                
                _logger.LogDebug($"JSON Root Kind: {root.ValueKind}");
                
                List<Importacion>? importaciones = null;
                
                if (root.ValueKind == JsonValueKind.Array)
                {
                    _logger.LogInformation("Detectado formato de array directo");
                    importaciones = JsonSerializer.Deserialize<List<Importacion>>(content, options);
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("value", out JsonElement valueElement))
                    {
                        _logger.LogInformation("Detectado formato con propiedad 'value'");
                        var importacionesJson = valueElement.GetRawText();
                        importaciones = JsonSerializer.Deserialize<List<Importacion>>(importacionesJson, options);
                    }
                }
                
                if (importaciones == null || !importaciones.Any())
                {
                    _logger.LogWarning("No se encontraron importaciones o no se pudo deserializar la respuesta");
                    return Enumerable.Empty<SelectListItem>();
                }

                _logger.LogInformation($"Importaciones deserializadas: {importaciones.Count}");
                
                for (int i = 0; i < Math.Min(3, importaciones.Count); i++)
                {
                    var item = importaciones[i];
                    _logger.LogDebug($"Item {i}: ID={item.id}, Barco={item.Barco?.nombrebarco ?? "null"}, Fecha={item.fechahora}");
                }
                
                return importaciones
                    .OrderByDescending(i => i.fechahora)
                    .Select(i => new SelectListItem
                    {
                        Value = i.id.ToString(),
                        Text = $"{i.Barco?.nombrebarco ?? "Sin barco"} - {i.fechahora:dd/MM/yyyy}"
                    }).ToList();
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
                _logger.LogInformation("Solicitando lista de empresas");
                var empresaUrl = "/api/Empresa/GetAll";
                var response = await _httpClient.GetAsync(empresaUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Error del API: {response.StatusCode}");
                    throw new HttpRequestException($"API error: {response.StatusCode}");
                }
                
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;
                
                _logger.LogDebug($"JSON Root Kind: {root.ValueKind}");
                
                List<Empresa>? empresas = null;
                
                if (root.ValueKind == JsonValueKind.Array)
                {
                    _logger.LogInformation("Detectado formato de array directo");
                    empresas = JsonSerializer.Deserialize<List<Empresa>>(content, options);
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("value", out JsonElement valueElement))
                    {
                        _logger.LogInformation("Detectado formato con propiedad 'value'");
                        var empresasJson = valueElement.GetRawText();
                        empresas = JsonSerializer.Deserialize<List<Empresa>>(empresasJson, options);
                    }
                }
                
                if (empresas == null || !empresas.Any())
                {
                    _logger.LogWarning("No se encontraron empresas o no se pudo deserializar la respuesta");
                    return Enumerable.Empty<SelectListItem>();
                }

                return empresas
                    .Where(e => e.estatus == 1)
                    .OrderBy(e => e.nombreempresa)
                    .Select(e => new SelectListItem
                    {
                        Value = e.id_empresa.ToString(),
                        Text = $"{e.id_empresa} - {e.nombreempresa ?? string.Empty}"
                    }).ToList();
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
                _logger.LogInformation($"Solicitando RegistroRequerimientos para barcoId={barcoId}");
                var url = $"{_apiBaseUrl}/RegistroRequerimientos?selectedBarco={barcoId}";
                _logger.LogDebug($"URL: {url}");

                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Error del API: {response.StatusCode}");
                    throw new HttpRequestException($"API error: {response.StatusCode}");
                }
                
                var content = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation($"Respuesta completa: {content}");

                if (string.IsNullOrWhiteSpace(content))
                {
                    _logger.LogWarning("La respuesta API está vacía");
                    return new List<RegistroRequerimientosViewModel>();
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                try
                {
                    using var document = JsonDocument.Parse(content);
                    var root = document.RootElement;
                    
                    _logger.LogDebug($"JSON Root Kind: {root.ValueKind}");
                    
                    List<RegistroRequerimientosViewModel> result = new List<RegistroRequerimientosViewModel>();
                    
                    if (root.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Array)
                    {
                        _logger.LogInformation("Encontrada propiedad 'data' con array directo");
                        
                        try {
                            result = JsonSerializer.Deserialize<List<RegistroRequerimientosViewModel>>(
                                dataElement.GetRawText(),
                                options
                            ) ?? new List<RegistroRequerimientosViewModel>();
                            _logger.LogInformation($"Deserializados {result.Count} registros desde data array");
                            return result;
                        }
                        catch (JsonException ex) {
                            _logger.LogWarning($"Error deserializando data array: {ex.Message}");
                        }
                    }
                    
                    if (root.TryGetProperty("data", out var dataObj) && 
                        dataObj.ValueKind == JsonValueKind.Object &&
                        dataObj.TryGetProperty("$values", out var valuesElement) && 
                        valuesElement.ValueKind == JsonValueKind.Array)
                    {
                        _logger.LogInformation("Encontrada estructura data.$values array");
                        try {
                            result = JsonSerializer.Deserialize<List<RegistroRequerimientosViewModel>>(
                                valuesElement.GetRawText(),
                                options
                            ) ?? new List<RegistroRequerimientosViewModel>();
                            _logger.LogInformation($"Deserializados {result.Count} registros desde $values");
                            return result;
                        }
                        catch (JsonException ex) {
                            _logger.LogWarning($"Error deserializando $values: {ex.Message}");
                        }
                    }
                    
                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        _logger.LogInformation("La raíz del JSON es un array directo");
                        try {
                            result = JsonSerializer.Deserialize<List<RegistroRequerimientosViewModel>>(
                                content,
                                options
                            ) ?? new List<RegistroRequerimientosViewModel>();
                            _logger.LogInformation($"Deserializados {result.Count} registros desde root array");
                            return result;
                        }
                        catch (JsonException ex) {
                            _logger.LogWarning($"Error deserializando root array: {ex.Message}");
                        }
                    }
                    
                    if (content.Contains("\"count\":0") || content.Contains("\"data\":[]"))
                    {
                        _logger.LogInformation("La respuesta indica conteo cero, retornando lista vacía");
                        return new List<RegistroRequerimientosViewModel>();
                    }
                    
                    if (root.TryGetProperty("data", out var dataElem))
                    {
                        _logger.LogInformation("Intentando parsing manual de datos");
                        result = new List<RegistroRequerimientosViewModel>();
                        
                        if (dataElem.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in dataElem.EnumerateArray())
                            {
                                try 
                                {
                                    var registro = new RegistroRequerimientosViewModel
                                    {
                                        IdMovimiento = GetIntPropertySafe(item, "idMovimiento"),
                                        FechaHora = GetDateTimePropertySafe(item, "fechaHora"),
                                        IdImportacion = GetIntPropertySafe(item, "idImportacion"),
                                        Importacion = GetStringPropertySafe(item, "importacion"),
                                        IdEmpresa = GetIntPropertySafe(item, "idEmpresa"),
                                        Empresa = GetStringPropertySafe(item, "empresa"),
                                        TipoTransaccion = GetIntPropertySafe(item, "tipoTransaccion"),
                                        CantidadRequerida = GetDecimalPropertySafe(item, "cantidadRequerida"),
                                        CantidadCamiones = GetIntPropertySafe(item, "cantidadCamiones")
                                    };
                                    result.Add(registro);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error procesando elemento individual");
                                }
                            }
                            _logger.LogInformation($"Manualmente procesados {result.Count} registros");
                            return result;
                        }
                    }
                    
                    _logger.LogWarning("No se pudo procesar la respuesta en ningún formato conocido");
                    return new List<RegistroRequerimientosViewModel>();
                }
                catch (JsonException jex)
                {
                    _logger.LogError(jex, "Error analizando el JSON de respuesta");
                    throw new Exception($"Error procesando la respuesta JSON: {jex.Message}", jex);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"Error HTTP al obtener registros para barcoId={barcoId}");
                throw new Exception($"Error al obtener registros: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error inesperado al obtener registros para barcoId={barcoId}");
                throw new Exception($"Error al obtener registros: {ex.Message}", ex);
            }
        }

        private int GetIntPropertySafe(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop) && 
                prop.ValueKind != JsonValueKind.Null &&
                prop.ValueKind != JsonValueKind.Undefined)
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var value))
                    return value;
                    
                if (prop.ValueKind == JsonValueKind.String && 
                    int.TryParse(prop.GetString(), out var parsedValue))
                    return parsedValue;
            }
            return 0;
        }

        private decimal GetDecimalPropertySafe(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop) && 
                prop.ValueKind != JsonValueKind.Null &&
                prop.ValueKind != JsonValueKind.Undefined)
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDecimal(out var value))
                    return value;
                    
                if (prop.ValueKind == JsonValueKind.String && 
                    decimal.TryParse(prop.GetString(), out var parsedValue))
                    return parsedValue;
            }
            return 0;
        }

        private string GetStringPropertySafe(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop) && 
                prop.ValueKind != JsonValueKind.Null &&
                prop.ValueKind != JsonValueKind.Undefined)
            {
                if (prop.ValueKind == JsonValueKind.String)
                    return prop.GetString() ?? string.Empty;
                
                return prop.ToString();
            }
            return string.Empty;
        }

        private DateTime? GetDateTimePropertySafe(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop) && 
                prop.ValueKind != JsonValueKind.Null &&
                prop.ValueKind != JsonValueKind.Undefined)
            {
                if (prop.TryGetDateTime(out var date))
                    return date;
                    
                if (prop.ValueKind == JsonValueKind.String && 
                    DateTime.TryParse(prop.GetString(), out var parsedDate))
                    return parsedDate;
            }
            return null;
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

                _logger.LogInformation($"Movimiento ID: {movimiento.id}, ImportacionID: {movimiento.idimportacion}, EmpresaID: {movimiento.idempresa}");

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
                            var rootElement = importacionDoc.RootElement;

                            Importacion? importacion = null;

                            if (rootElement.TryGetProperty("value", out var impValueElement))
                            {
                                importacion = JsonSerializer.Deserialize<Importacion>(impValueElement.GetRawText(), options);
                            }
                            else
                            {
                                importacion = JsonSerializer.Deserialize<Importacion>(importacionContent, options);
                            }

                            if (importacion != null)
                            {
                                if (importacion.Barco != null && !string.IsNullOrEmpty(importacion.Barco.nombrebarco))
                                {
                                    importacionNombre = $"{importacion.Barco.nombrebarco} - {importacion.fechahora:dd/MM/yyyy}";
                                }
                                else
                                {
                                    if (rootElement.TryGetProperty("barco", out var barcoElement) && 
                                        barcoElement.TryGetProperty("nombrebarco", out var nombreBarcoElement))
                                    {
                                        string nombreBarco = nombreBarcoElement.GetString() ?? "Sin nombre";
                                        importacionNombre = $"{nombreBarco} - {importacion.fechahora:dd/MM/yyyy}";
                                    }
                                    else
                                    {
                                        importacionNombre = $"Importación #{movimiento.idimportacion} - {importacion.fechahora:dd/MM/yyyy}";
                                    }
                                }
                                _logger.LogInformation($"Nombre importación encontrado: {importacionNombre}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al obtener detalles de la importación");
                        importacionNombre = $"Importación #{movimiento.idimportacion}";
                    }
                }

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
                            var rootElement = empresaDoc.RootElement;

                            Empresa? empresa = null;

                            if (rootElement.TryGetProperty("value", out var empValueElement))
                            {
                                empresa = JsonSerializer.Deserialize<Empresa>(empValueElement.GetRawText(), options);
                            }
                            else
                            {
                                empresa = JsonSerializer.Deserialize<Empresa>(empresaContent, options);
                            }

                            if (empresa != null)
                            {
                                if (!string.IsNullOrEmpty(empresa.nombreempresa))
                                {
                                    empresaNombre = empresa.nombreempresa;
                                }
                                else
                                {
                                    if (rootElement.TryGetProperty("nombreempresa", out var nombreElement))
                                    {
                                        empresaNombre = nombreElement.GetString() ?? $"Empresa #{movimiento.idempresa}";
                                    }
                                    else
                                    {
                                        empresaNombre = $"Empresa #{movimiento.idempresa}";
                                    }
                                }
                                _logger.LogInformation($"Nombre empresa encontrado: {empresaNombre}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al obtener detalles de la empresa");
                        empresaNombre = $"Empresa #{movimiento.idempresa}";
                    }
                }

                var viewModel = new RegistroRequerimientosViewModel
                {
                    IdMovimiento = movimiento.id,
                    FechaHora = movimiento.fechahora,
                    IdImportacion = movimiento.idimportacion,
                    IdEmpresa = movimiento.idempresa,
                    TipoTransaccion = movimiento.tipotransaccion,
                    CantidadRequerida = movimiento.cantidadrequerida ?? 0m, 
                    CantidadCamiones = movimiento.cantidadcamiones ?? 0,    
                    Importacion = importacionNombre,
                    Empresa = empresaNombre
                };

                _logger.LogInformation($"ViewModel preparado - IdMovimiento: {viewModel.IdMovimiento}, " +
                                      $"Importacion: '{viewModel.Importacion}', Empresa: '{viewModel.Empresa}'");

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

        public async Task<List<InformeGeneralViewModel>> GetInformeGeneralAsync(int id)
        {
            try
            {
                _logger.LogInformation($"Solicitando informe general para barcoId={id}");
                var url = $"{_apiBaseUrl}/InformeGeneral?importacionId={id}&fields=empresaId,empresa,requeridoKg,requeridoTon,descargaKg,faltanteKg,tonFaltantes,conteoPlacas,porcentajeDescarga,camionesFaltantes";
                _logger.LogDebug($"URL: {url}");
                
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Error del API: {response.StatusCode}");
                    throw new HttpRequestException($"API error: {response.StatusCode}");
                }
                
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug($"Respuesta: {content.Substring(0, Math.Min(100, content.Length))}...");
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;
                
                _logger.LogDebug($"JSON Root Kind: {root.ValueKind}");

                if (root.TryGetProperty("totalMovimientos", out var totalMovimientosElement))
                {
                    TotalMovimientos = totalMovimientosElement.GetInt32();
                    _logger.LogInformation($"Total movimientos from API: {TotalMovimientos}");
                }

                if (root.TryGetProperty("data", out var dataElement))
                {
                    _logger.LogInformation("Encontrada propiedad 'data' en respuesta");
                    var informes = JsonSerializer.Deserialize<List<InformeGeneralViewModel>>(
                        dataElement.GetRawText(),
                        options
                    );

                    _logger.LogInformation($"Deserializados {informes?.Count ?? 0} informes");
                    return informes ?? new List<InformeGeneralViewModel>();
                }
                
                if (root.ValueKind == JsonValueKind.Array)
                {
                    _logger.LogInformation("Intentando deserializar array directamente");
                    var informes = JsonSerializer.Deserialize<List<InformeGeneralViewModel>>(content, options);
                    return informes ?? new List<InformeGeneralViewModel>();
                }

                _logger.LogWarning("No se pudo encontrar datos válidos en la respuesta");
                throw new Exception($"Estructura de respuesta inválida");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo informe general para barco {BarcoId}", id);
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

                var movimiento = new Movimiento
                {
                    id = viewModel.IdMovimiento ?? throw new InvalidOperationException("IdMovimiento cannot be null"),
                    idimportacion = viewModel.IdImportacion ?? 0,
                    idempresa = viewModel.IdEmpresa,
                    tipotransaccion = viewModel.TipoTransaccion ?? 1,
                    cantidadrequerida = viewModel.CantidadRequerida,
                    cantidadcamiones = viewModel.CantidadCamiones,
                    fechahora = viewModel.FechaHora ?? DateTime.Now,
                    idusuario = int.TryParse(userId, out int parsedId) ? parsedId : null
                };

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
                _logger.LogInformation($"Solicitando datos de escotillas para importación {importacionId}");
                var url = $"{_apiBaseUrl}/CalculoEscotillas?importacionId={importacionId}";
                _logger.LogDebug($"URL: {url}");
                
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Error del API: {response.StatusCode}");
                    throw new HttpRequestException($"API error: {response.StatusCode}");
                }
                
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug($"Respuesta: {content.Substring(0, Math.Min(100, content.Length))}...");
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<EscotillaApiResponse>(content, options)
                    ?? new EscotillaApiResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener datos de escotillas para importación {ImportacionId}", importacionId);
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
                    EstadoGeneral = apiResponse?.Totales?.EstadoGeneral ?? "Sin información",
                    NombreBarco = apiResponse?.NombreBarco ?? "Sin nombre"

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
                _logger.LogDebug($"URL: {url}");
                
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Error del API: {response.StatusCode}");
                    throw new HttpRequestException($"API error: {response.StatusCode}");
                }
                
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug($"Respuesta recibida. Longitud: {content.Length} caracteres");
                
                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;
                
                _logger.LogDebug($"JSON Root Kind: {root.ValueKind}");

                if (root.TryGetProperty("data", out var dataElement))
                {
                    _logger.LogInformation("Encontrada propiedad 'data' en respuesta");
                    
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
                                    PesoEntregado = GetDecimalValue(element, "pesoEntregado"),
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
                
                if (root.ValueKind == JsonValueKind.Array)
                {
                    _logger.LogInformation("Intentando deserializar array directamente");
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    var resultados = JsonSerializer.Deserialize<List<RegistroPesajesIndividual>>(content, options);
                    return resultados ?? new List<RegistroPesajesIndividual>();
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
                _logger.LogInformation($"Solicitando datos de escotillas por empresa para importación {importacionId}");
                var url = $"{_apiBaseUrl}/CalculoEscotillasPorEmpresa?importacionId={importacionId}";
                _logger.LogDebug($"URL: {url}");
                
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Error del API: {response.StatusCode}");
                    throw new HttpRequestException($"API error: {response.StatusCode}");
                }
                
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug($"Respuesta recibida. Longitud: {content.Length} caracteres");
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                using JsonDocument document = JsonDocument.Parse(content);
                var root = document.RootElement;
                
                _logger.LogDebug($"JSON Root Kind: {root.ValueKind}");
                
                var viewModel = new ReporteEscotillasPorEmpresaViewModel();

                if (root.TryGetProperty("empresas", out JsonElement empresasElement))
                {
                    _logger.LogInformation("Encontrada propiedad 'empresas' en respuesta");
                    viewModel.Empresas = JsonSerializer.Deserialize<List<EmpresaEscotillasViewModel>>(
                        empresasElement.GetRawText(), options) ?? new List<EmpresaEscotillasViewModel>();
                    return viewModel;
                }
                
                if (root.ValueKind == JsonValueKind.Array)
                {
                    _logger.LogInformation("Intentando deserializar array directamente como empresas");
                    viewModel.Empresas = JsonSerializer.Deserialize<List<EmpresaEscotillasViewModel>>(content, options) 
                        ?? new List<EmpresaEscotillasViewModel>();
                    return viewModel;
                }

                _logger.LogWarning("No se encontraron datos válidos de escotillas por empresa");
                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener datos de escotillas por empresa");
                throw;
            }
        }

        public async Task<IEnumerable<SelectListItem>> GetEmpresasWithMovimientosAsync(int importacionId)
        {
            try
            {
                _logger.LogInformation($"Obteniendo empresas con movimientos para importación {importacionId}");
                var url = $"{_apiBaseUrl}/InformeGeneral?importacionId={importacionId}";
                _logger.LogDebug($"URL: {url}");

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Error: {response.StatusCode}");
                    return Enumerable.Empty<SelectListItem>();
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug($"Respuesta obtenida, longitud: {content.Length}");

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;
                
                _logger.LogDebug($"JSON Root Kind: {root.ValueKind}");

                if (root.TryGetProperty("data", out var dataElement))
                {
                    _logger.LogInformation("Encontrada propiedad 'data' en respuesta");
                    var empresasIds = new HashSet<int>();
                    var empresasNombres = new Dictionary<int, string>();

                    foreach (var item in dataElement.EnumerateArray())
                    {
                        if (item.TryGetProperty("empresaId", out var idElement) ||
                            item.TryGetProperty("id_empresa", out idElement))
                        {
                            int empresaId = idElement.GetInt32();
                            empresasIds.Add(empresaId);

                            if (item.TryGetProperty("empresa", out var nombreElement))
                            {
                                empresasNombres[empresaId] = nombreElement.GetString() ?? "Sin nombre";
                            }
                        }
                    }

                    _logger.LogInformation($"Encontradas {empresasIds.Count} empresas con movimientos");

                    var todasLasEmpresas = await GetEmpresasSelectListAsync();

                    return todasLasEmpresas
                        .Where(e => int.TryParse(e.Value, out var id) && empresasIds.Contains(id))
                        .ToList();
                }
                
                if (root.ValueKind == JsonValueKind.Array)
                {
                    _logger.LogInformation("Procesando array directo de empresas con movimientos");
                    var empresasIds = new HashSet<int>();
                    
                    foreach (var item in root.EnumerateArray())
                    {
                        if (item.TryGetProperty("empresaId", out var idElement) ||
                            item.TryGetProperty("id_empresa", out idElement))
                        {
                            int empresaId = idElement.GetInt32();
                            empresasIds.Add(empresaId);
                        }
                    }
                    
                    _logger.LogInformation($"Encontradas {empresasIds.Count} empresas con movimientos (formato directo)");
                    
                    var todasLasEmpresas = await GetEmpresasSelectListAsync();
                    
                    return todasLasEmpresas
                        .Where(e => int.TryParse(e.Value, out var id) && empresasIds.Contains(id))
                        .ToList();
                }

                _logger.LogWarning("No se encontraron datos válidos de empresas con movimientos");
                return Enumerable.Empty<SelectListItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener empresas con movimientos");
                return Enumerable.Empty<SelectListItem>();
            }
        }
    }
}