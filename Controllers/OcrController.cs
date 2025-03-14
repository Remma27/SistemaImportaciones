using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Sistema_de_Gestion_de_Importaciones.Controllers.Api
{
    [Route("api/ocr")]
    [ApiController]
    public class OcrController : ControllerBase
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OcrController> _logger;

        public OcrController(
            IHttpClientFactory clientFactory,
            IConfiguration configuration,
            ILogger<OcrController> logger)
        {
            _clientFactory = clientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessImage([FromBody] OcrRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ImageData))
                {
                    return BadRequest("Imagen no proporcionada");
                }

                // Extraer datos base64 excluyendo el prefijo (ej: "data:image/jpeg;base64,")
                string base64Data = request.ImageData;
                if (base64Data.Contains(","))
                {
                    base64Data = base64Data.Substring(base64Data.IndexOf(",") + 1);
                }

                // Convertir imagen Base64 a bytes
                byte[] imageBytes = Convert.FromBase64String(base64Data);

                // Puedes usar uno de estos servicios según tu preferencia:
                // Opción 1: OCR.Space API (más sencillo de implementar)
                var extractedText = await UseOcrSpaceApi(imageBytes);

                // Opción 2: Mistral AI API (más avanzado, requiere cuenta)
                // var extractedText = await UseMistralApi(imageBytes);

                // Análisis de texto para extraer información relevante
                var result = ExtractRelevantInformation(extractedText);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando OCR");
                return StatusCode(500, new { error = "Error interno procesando la imagen" });
            }
        }

        private async Task<string> UseOcrSpaceApi(byte[] imageBytes)
        {
            using var client = _clientFactory.CreateClient();
            // Obtén tu clave API gratuita en https://ocr.space/OCRAPI
            string apiKey = _configuration["OcrSpace:ApiKey"] ?? "helloworld"; // Clave por defecto para test

            using var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(imageBytes), "file", "image.jpg");
            content.Add(new StringContent(apiKey), "apikey");
            content.Add(new StringContent("spa"), "language"); // Español
            content.Add(new StringContent("true"), "detectOrientation");

            var response = await client.PostAsync("https://api.ocr.space/parse/image", content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<OcrSpaceResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result?.ParsedResults?.Count > 0)
            {
                return result.ParsedResults[0].ParsedText ?? string.Empty;
            }

            return string.Empty;
        }

        private async Task<string> UseMistralApi(byte[] imageBytes)
        {
            // Nota: Esta es una implementación de ejemplo. Mistral AI está en desarrollo
            // y podría requerir ajustes según la documentación oficial.

            using var client = _clientFactory.CreateClient();
            string apiKey = _configuration["Mistral:ApiKey"] ?? string.Empty;

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var base64Image = Convert.ToBase64String(imageBytes);

            var request = new
            {
                model = "mistral-medium",
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = "Extrae toda la información de texto visible en esta imagen, especialmente números de guía, placas y pesos." },
                            new { type = "image_url", image_url = new { url = $"data:image/jpeg;base64,{base64Image}" } }
                        }
                    }
                },
                max_tokens = 500
            };

            var content = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api.mistral.ai/v1/chat/completions", content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(responseBody);

            return result.RootElement.GetProperty("choices")[0].GetProperty("message")
                .GetProperty("content").GetString() ?? string.Empty;
        }

        private OcrResult ExtractRelevantInformation(string text)
        {
            var result = new OcrResult();

            if (string.IsNullOrEmpty(text))
                return result;

            // Patrones regex para buscar información
            var guiaPattern = @"(?:guia|guía|remisión|remision|documento)[\s:]*([A-Z0-9]{5,15})";
            var placaPattern = @"(?:placa|camión|camion|vehículo|vehiculo)[\s:]*([A-Z0-9]{5,8})";
            var pesoPattern = @"(?:peso|kilos|kg)[\s:]*(\d+[,.]?\d*)";

            // Buscar coincidencias
            var guiaMatch = Regex.Match(text, guiaPattern, RegexOptions.IgnoreCase);
            if (guiaMatch.Success)
                result.Guia = guiaMatch.Groups[1].Value.Trim();

            var placaMatch = Regex.Match(text, placaPattern, RegexOptions.IgnoreCase);
            if (placaMatch.Success)
                result.Placa = placaMatch.Groups[1].Value.Trim();

            var pesoMatch = Regex.Match(text, pesoPattern, RegexOptions.IgnoreCase);
            if (pesoMatch.Success)
            {
                if (double.TryParse(pesoMatch.Groups[1].Value.Replace(',', '.'), out double peso))
                    result.Peso = peso;
            }

            // Búsqueda de información adicional basada en contexto
            var lines = text.Split('\n');
            foreach (var line in lines)
            {
                // Si no encontramos guía y esta línea parece contener un código alfanumérico
                if (string.IsNullOrEmpty(result.Guia) && Regex.IsMatch(line, @"[A-Z0-9]{5,15}"))
                {
                    var match = Regex.Match(line, @"([A-Z0-9]{5,15})");
                    if (match.Success)
                        result.Guia = match.Groups[1].Value;
                }

                // Si esta línea parece contener una placa y no tenemos placa o placa alterna
                if (Regex.IsMatch(line, @"[A-Z]{3}[-\s]?\d{3,4}"))
                {
                    var match = Regex.Match(line, @"([A-Z]{3}[-\s]?\d{3,4})");
                    if (match.Success)
                    {
                        if (string.IsNullOrEmpty(result.Placa))
                            result.Placa = match.Groups[1].Value.Replace(" ", "").Replace("-", "");
                        else if (string.IsNullOrEmpty(result.PlacaAlterna))
                            result.PlacaAlterna = match.Groups[1].Value.Replace(" ", "").Replace("-", "");
                    }
                }
            }

            return result;
        }
    }

    // Clases para solicitudes y respuestas
    public class OcrRequest
    {
        public string? ImageData { get; set; }
    }

    public class OcrResult
    {
        public string? Guia { get; set; }
        public string? GuiaAlterna { get; set; }
        public string? Placa { get; set; }
        public string? PlacaAlterna { get; set; }
        public double? Peso { get; set; }
    }

    // Clases para deserializar la respuesta de OCR.Space
    public class OcrSpaceResponse
    {
        public List<ParsedResult>? ParsedResults { get; set; }
    }

    public class ParsedResult
    {
        public string? ParsedText { get; set; }
    }
}
