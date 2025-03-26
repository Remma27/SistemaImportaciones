using API.Models;
using System.Text.Json;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;

namespace Sistema_de_Gestion_de_Importaciones.Services
{
    public class EmpresaService : IEmpresaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;
        private readonly ILogger<EmpresaService> _logger;

        public EmpresaService(HttpClient httpClient, IConfiguration configuration, ILogger<EmpresaService> logger)
        {
            _httpClient = httpClient;
            _apiBaseUrl = "api/Empresa/";
            _logger = logger;
        }

        public async Task<IEnumerable<Empresa>> GetAllAsync()
        {
            try
            {
                _logger.LogInformation("Iniciando solicitud GetAllAsync");
                var url = _apiBaseUrl + "GetAll";

                var response = await _httpClient.GetAsync(url);
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
                    var empresas = JsonSerializer.Deserialize<List<Empresa>>(content, options);
                    _logger.LogInformation($"Empresas deserializadas: {empresas?.Count ?? 0}");
                    return empresas ?? new List<Empresa>();
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("value", out JsonElement valueElement))
                    {
                        _logger.LogInformation("Detectado formato con propiedad 'value'");
                        var empresasJson = valueElement.GetRawText();
                        var empresas = JsonSerializer.Deserialize<List<Empresa>>(empresasJson, options);
                        return empresas ?? new List<Empresa>();
                    }
                    else
                    {
                        _logger.LogInformation("Intentando deserialización con Newtonsoft.Json");
                        try
                        {
                            var empresas = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Empresa>>(content);
                            if (empresas != null)
                            {
                                _logger.LogInformation($"Empresas deserializadas con Newtonsoft: {empresas.Count}");
                                return empresas;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error con Newtonsoft.Json");
                        }
                    }
                }

                _logger.LogWarning("No se pudo deserializar la respuesta en ningún formato conocido");
                return new List<Empresa>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetAllAsync");
                throw;
            }
        }

        public async Task<Empresa?> GetByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation($"Iniciando solicitud GetByIdAsync para id={id}");
                var url = _apiBaseUrl + $"Get?id={id}";

                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Respuesta recibida: Status={response.StatusCode}, Content={content}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Error del API: {response.StatusCode}, Content={content}");
                    throw new HttpRequestException($"API error: {response.StatusCode}");
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                using JsonDocument jsonDocument = JsonDocument.Parse(content);
                Empresa? empresa = null;

                if (jsonDocument.RootElement.TryGetProperty("value", out JsonElement valueElement))
                {
                    empresa = JsonSerializer.Deserialize<Empresa>(valueElement.GetRawText(), options);
                }
                else
                {
                    empresa = JsonSerializer.Deserialize<Empresa>(content, options);
                }

                return empresa;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener empresa con ID {id}");
                throw;
            }
        }

        public async Task<Empresa> CreateAsync(Empresa empresa)
        {
            var response = await _httpClient.PostAsJsonAsync(_apiBaseUrl + "Create", empresa);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Empresa>() ?? empresa;
        }

        public async Task<Empresa> UpdateAsync(int id, Empresa empresa)
        {
            try
            {
                _logger.LogInformation($"Actualizando empresa con ID {id}");
                var response = await _httpClient.PutAsJsonAsync(_apiBaseUrl + $"Edit", empresa);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Respuesta del servidor: {content}");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return await response.Content.ReadFromJsonAsync<Empresa>(options) ?? empresa;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar empresa con ID {id}");
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                var url = _apiBaseUrl + $"Delete?id={id}";
                _logger.LogInformation($"Eliminando empresa con ID={id}, URL: {url}");
                
                var response = await _httpClient.DeleteAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Error al eliminar empresa: Status={response.StatusCode}, Content={content}");
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        try
                        {
                            using (JsonDocument doc = JsonDocument.Parse(content))
                            {
                                if (doc.RootElement.TryGetProperty("message", out JsonElement messageElement))
                                {
                                    string mensaje = messageElement.GetString() ?? "No se puede eliminar la empresa";
                                    
                                    if (mensaje.Contains("importaciones") || mensaje.Contains("movimientos") || 
                                        mensaje.Contains("asociada") || mensaje.Contains("relacionada"))
                                    {
                                        _logger.LogWarning($"Empresa con ID={id} tiene relaciones: {mensaje}");
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
                        
                        if (content.Contains("importaciones") || content.Contains("movimientos") || 
                            content.Contains("asociada") || content.Contains("relacionada"))
                        {
                            throw new InvalidOperationException("No se puede eliminar esta empresa porque tiene importaciones o movimientos asociados.");
                        }
                    }
                    
                    throw new HttpRequestException($"Error al eliminar empresa: {response.StatusCode}, {content}");
                }
                
                _logger.LogInformation($"Empresa con ID={id} eliminada exitosamente");
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
                _logger.LogError(ex, $"Error inesperado al eliminar la empresa con ID={id}");
                throw new Exception($"Error al eliminar la empresa: {ex.Message}", ex);
            }
        }
    }
}