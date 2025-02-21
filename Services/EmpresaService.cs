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

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Error del API: {response.StatusCode}, Content={content}");
                    throw new HttpRequestException($"API error: {response.StatusCode}");
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // Deserializar primero a un objeto dinámico para acceder a la propiedad "value"
                using JsonDocument document = JsonDocument.Parse(content);
                var root = document.RootElement;

                if (root.TryGetProperty("value", out JsonElement valueElement))
                {
                    var empresasJson = valueElement.GetRawText();
                    var empresas = JsonSerializer.Deserialize<IEnumerable<Empresa>>(empresasJson, options);
                    _logger.LogInformation($"Empresas deserializadas: {empresas?.Count() ?? 0}");
                    return empresas ?? new List<Empresa>();
                }

                _logger.LogWarning("No se encontró la propiedad 'value' en la respuesta");
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

                // Si existe la propiedad "value", la usamos; de lo contrario, deserializamos el documento completo.
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
            var url = _apiBaseUrl + $"Delete?id={id}";
            _logger.LogInformation($"Eliminando empresa con URL: {url}");
            var response = await _httpClient.DeleteAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Error al eliminar: Status={response.StatusCode}. Detalles: {content}");
            }
        }
    }
}