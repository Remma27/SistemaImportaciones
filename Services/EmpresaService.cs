using System.Net.Http.Json;
using API.Models;
using System.Text.Json;
using System.Diagnostics;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using Microsoft.Extensions.Logging;

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
            return await _httpClient.GetFromJsonAsync<Empresa>(_apiBaseUrl + $"Get?id={id}");
        }

        public async Task<Empresa> CreateAsync(Empresa empresa)
        {
            var response = await _httpClient.PostAsJsonAsync(_apiBaseUrl + "Create", empresa);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Empresa>() ?? empresa;
        }

        public async Task<Empresa> UpdateAsync(int id, Empresa empresa)
        {
            var response = await _httpClient.PutAsJsonAsync(_apiBaseUrl + "Edit", empresa);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Empresa>() ?? empresa;
        }

        public async Task DeleteAsync(int id)
        {
            var response = await _httpClient.DeleteAsync(_apiBaseUrl + $"Delete?id={id}");
            response.EnsureSuccessStatusCode();
        }
    }
}