using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using Sistema_de_Gestion_de_Importaciones.ViewModels;
using System.Net.Http.Json;

namespace Sistema_de_Gestion_de_Importaciones.Services
{
    public class HistorialService : IHistorialService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HistorialService> _logger;

        public HistorialService(HttpClient httpClient, IConfiguration configuration, ILogger<HistorialService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<HistorialResponseViewModel> ObtenerTodoAsync()
        {
            try
            {
                _logger.LogInformation("Obteniendo todos los registros de historial sin filtrado");
                var response = await _httpClient.GetAsync("api/Historial/GetAll");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Respuesta JSON recibida: {Length} bytes", content.Length);
                
                var resultado = JsonConvert.DeserializeObject<HistorialResponseViewModel>(content);
                
                if (resultado != null)
                {
                    _logger.LogInformation("Registros de historial obtenidos: {Count}", resultado.TotalRegistros);
                    
                    // Verificar los IDs recibidos para depuración
                    if (resultado.Registros != null && resultado.Registros.Any())
                    {
                        var ids = resultado.Registros.Select(r => r.Id).Take(10).ToList();
                        _logger.LogInformation("Primeros 10 IDs: {Ids}", string.Join(", ", ids));
                        
                        // Verificar si hay algún registro con ID=1
                        var tieneId1 = resultado.Registros.Any(r => r.Id == 1);
                        _logger.LogInformation("¿Contiene registro con ID=1? {TieneId1}", tieneId1);
                    }
                }
                
                return resultado ?? new HistorialResponseViewModel();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener historial");
                return new HistorialResponseViewModel();
            }
        }

        public async Task<HistorialViewModel> ObtenerPorIdAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/Historial/GetById?id={id}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var resultado = JsonConvert.DeserializeObject<HistorialViewModel>(content);
                return resultado ?? new HistorialViewModel();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener historial por ID {Id}", id);
                return new HistorialViewModel();
            }
        }

        public async Task<List<string>> ObtenerTablasDisponiblesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/Historial/GetTablasDisponibles");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var resultado = JsonConvert.DeserializeObject<List<string>>(content);
                return resultado ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tablas disponibles");
                return new List<string>();
            }
        }
    }
}
