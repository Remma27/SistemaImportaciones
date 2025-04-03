using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sistema_de_Gestion_de_Importaciones.Extensions;

namespace Sistema_de_Gestion_de_Importaciones.Controllers
{
    [Authorize]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class SincronizacionController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<SincronizacionController> _logger;

        public SincronizacionController(IHttpClientFactory clientFactory, ILogger<SincronizacionController> logger)
        {
            _clientFactory = clientFactory;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ExportarDatos()
        {
            try
            {
                _logger.LogInformation("Iniciando exportación completa de datos");
                
                var client = _clientFactory.CreateClient("ApiClient");
                string url = "api/Sincronizacion/ExportarDatos";
                
                // Use direct stream download to handle potentially large files
                var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                
                if (response.IsSuccessStatusCode)
                {
                    var contentDisposition = response.Content.Headers.ContentDisposition;
                    string fileName = contentDisposition?.FileName ?? $"Sincronizacion_{DateTime.Now:yyyyMMddHHmmss}.json.gz";
                    
                    // Strip quotes if present
                    fileName = fileName.Trim('"');
                    
                    // Set the content type to match the API's compressed response
                    Response.ContentType = "application/gzip";
                    Response.Headers.Append("Content-Disposition", $"attachment; filename={fileName}");
                    
                    // Stream the response directly to the client
                    await response.Content.CopyToAsync(Response.Body);
                    return new EmptyResult();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error al exportar datos: {StatusCode} - {Error}", response.StatusCode, error);
                    this.Error($"Error al exportar datos: {response.ReasonPhrase}");
                    return RedirectToAction("Index", "Sincronizacion", new { area = "" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar datos para sincronización");
                this.Error($"Error en la exportación: {ex.Message}");
                return RedirectToAction("Index", "Sincronizacion", new { area = "" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ImportarDatos(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
            {
                this.Error("No se ha seleccionado ningún archivo");
                return Redirect("/mvc/Sincronizacion/Index");
            }

            try
            {
                var client = _clientFactory.CreateClient("ApiClient");
                
                using var content = new MultipartFormDataContent();
                using var fileStream = archivo.OpenReadStream();
                using var streamContent = new StreamContent(fileStream);
                
                // Set content type based on file extension
                string contentType = archivo.FileName.EndsWith(".gz", StringComparison.OrdinalIgnoreCase) ?
                    "application/gzip" : "application/json";
                    
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                content.Add(streamContent, "archivo", archivo.FileName);
                
                var response = await client.PostAsync("api/Sincronizacion/ImportarDatos", content);
                
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    
                    try
                    {
                        // Properly deserialize the JSON response
                        var resultadoJson = JsonSerializer.Deserialize<ResultadoSincronizacion>(
                            responseContent, 
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        
                        // Add success message to toast
                        if (resultadoJson != null && resultadoJson.Exitoso)
                        {
                            this.Success($"Sincronización completada: {resultadoJson.Agregados} registros agregados, " +
                                       $"{resultadoJson.Actualizados} actualizados, " +
                                       $"{resultadoJson.Omitidos} sin cambios.");
                            
                            // Show warnings if there were errors
                            if (resultadoJson.ErroresCount > 0 && resultadoJson.Errores?.Count > 0)
                            {
                                this.Warning($"Ocurrieron {resultadoJson.ErroresCount} errores durante la importación.");
                            }
                        }
                        else
                        {
                            // Handle unsuccessful import but valid response
                            this.Warning(resultadoJson?.Mensaje ?? "Sincronización completada con advertencias");
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, "Error al deserializar respuesta de importación: {Response}", responseContent);
                        this.Error("Error al procesar la respuesta del servidor");
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error al importar datos: {StatusCode} - {Error}", response.StatusCode, error);
                    this.Error($"Error al importar datos: {response.ReasonPhrase}");
                }
                
                return Redirect("/mvc/Sincronizacion/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al importar archivo para sincronización");
                this.Error($"Error en la importación: {ex.Message}");
                return Redirect("/mvc/Sincronizacion/Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> EnviarCorreo(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                this.Error("La dirección de correo electrónico no es válida");
                return RedirectToAction("Index", "Sincronizacion", new { area = "" });
            }

            try
            {
                var client = _clientFactory.CreateClient("ApiClient");
                
                // The API expects a JSON object with a "Destinatario" property
                var payload = new { Destinatario = email };
                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");
                
                var response = await client.PostAsync("api/Sincronizacion/EnviarPorCorreo", content);
                
                if (response.IsSuccessStatusCode)
                {
                    this.Success($"Se ha enviado un correo de sincronización a {email}");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error al enviar correo: {StatusCode} - {Error}", response.StatusCode, error);
                    this.Error($"Error al enviar correo: {response.ReasonPhrase}");
                }
                
                return RedirectToAction("Index", "Sincronizacion", new { area = "" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correo de sincronización");
                this.Error($"Error al enviar correo: {ex.Message}");
                return RedirectToAction("Index", "Sincronizacion", new { area = "" });
            }
        }

        private class ResultadoSincronizacion
        {
            public bool Exitoso { get; set; }
            public string Mensaje { get; set; } = "";
            public int Agregados { get; set; }
            public int Actualizados { get; set; }
            public int Omitidos { get; set; }
            public DateTime? FechaSincronizacion { get; set; }
            public int ErroresCount { get; set; }
            public List<string> Errores { get; set; } = new List<string>();
        }
    }
}