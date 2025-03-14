using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using Tesseract;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

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

        private byte[] PreprocessImage(byte[] imageBytes)
        {
            try
            {
                using var ms = new MemoryStream(imageBytes);
                using var image = SixLabors.ImageSharp.Image.Load(ms);
                
                // Mejorar contraste de manera más agresiva
                image.Mutate(x => x
                    .Grayscale() // Convertir a escala de grises
                    .Brightness(0.1f) // Ajustar brillo 
                    .Contrast(0.3f) // Mayor contraste
                    .GaussianSharpen(0.5f)); // Aumentar nitidez
                
                // Guardar resultado procesado
                using var outStream = new MemoryStream();
                image.Save(outStream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder());
                return outStream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error preprocesando imagen: {ex.Message}");
                return imageBytes; // Devolver imagen original si hay error
            }
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

                // Log para diagnóstico inicial
                _logger.LogInformation("Iniciando proceso de OCR");

                // Limpiar Base64 si contiene prefijo de datos
                string base64Data = request.ImageData;
                if (base64Data.Contains(","))
                {
                    base64Data = base64Data.Substring(base64Data.IndexOf(",") + 1);
                }
                
                // Convertir imagen Base64 a bytes con manejo de excepciones
                byte[] imageBytes;
                try {
                    imageBytes = Convert.FromBase64String(base64Data);
                    _logger.LogInformation($"Imagen recibida: {imageBytes.Length} bytes");
                }
                catch (FormatException ex) {
                    _logger.LogError($"Error en formato Base64: {ex.Message}");
                    return BadRequest(new { error = "Formato de imagen inválido", details = ex.Message });
                }
                
                // Preprocesar la imagen (mejor contraste, etc)
                _logger.LogInformation("Preprocesando imagen...");
                byte[] processedImageBytes = PreprocessImage(imageBytes);
                
                // Verificar archivo de idioma - debe existir antes de continuar
                string tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
                string langFile = Path.Combine(tessDataPath, "spa.traineddata");
                if (!System.IO.File.Exists(langFile))
                {
                    _logger.LogError($"Archivo de idioma español no encontrado: {langFile}");
                    return StatusCode(500, new { 
                        error = "Configuración de OCR incompleta", 
                        details = "Faltan archivos de datos de idioma español para Tesseract" 
                    });
                }

                _logger.LogInformation("Iniciando OCR...");
                string extractedText;
                try {
                    extractedText = await UseTesseractOcr(processedImageBytes);
                }
                catch (Exception ex) {
                    _logger.LogError($"Error en OCR: {ex.GetType().Name} - {ex.Message}");
                    return StatusCode(500, new { 
                        error = "Error procesando OCR", 
                        details = ex.Message,
                        type = ex.GetType().Name
                    });
                }
                
                if (string.IsNullOrWhiteSpace(extractedText)) {
                    _logger.LogWarning("El OCR no extrajo ningún texto");
                    return StatusCode(422, new { 
                        error = "No se pudo extraer texto", 
                        details = "La imagen no contiene texto legible o no es adecuada para OCR" 
                    });
                }

                _logger.LogInformation("Extrayendo información relevante del texto...");
                var result = ExtractRelevantInformation(extractedText);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado procesando OCR");
                return StatusCode(500, new { 
                    error = $"Error inesperado: {ex.GetType().Name}", 
                    details = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        private async Task<string> UseTesseractOcr(byte[] imageBytes)
        {
            try
            {
                _logger.LogInformation($"Iniciando OCR con Tesseract para imagen de {imageBytes.Length} bytes");
                
                // Ejecutar el procesamiento OCR en un hilo en segundo plano
                return await Task.Run(() => {
                    string tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
                    _logger.LogInformation($"Ruta de tessdata: {tessDataPath}");
                    
                    // Verificar que el directorio existe
                    if (!Directory.Exists(tessDataPath))
                    {
                        _logger.LogWarning($"Directorio tessdata no encontrado, creándolo: {tessDataPath}");
                        Directory.CreateDirectory(tessDataPath);
                    }
                    
                    // Verificar que el archivo de idioma existe
                    string langFile = Path.Combine(tessDataPath, "spa.traineddata");
                    if (!System.IO.File.Exists(langFile))
                    {
                        _logger.LogError($"Archivo de idioma no encontrado: {langFile}");
                        throw new FileNotFoundException($"No se encontró el archivo de datos para el idioma español: {langFile}");
                    }
                    
                    // Guardar imagen en formato TIFF para mejor compatibilidad con Tesseract
                    using var ms = new MemoryStream(imageBytes);
                    using var image = SixLabors.ImageSharp.Image.Load(ms);
                    
                    // Mejorar imagen para OCR
                    image.Mutate(x => x
                        .Grayscale()
                        .Brightness(0.2f)
                        .Contrast(0.4f)
                        .GaussianSharpen(0.8f));
                    
                    string tempFile = Path.Combine(Path.GetTempPath(), $"ocr_temp_{Guid.NewGuid()}.tiff");
                    _logger.LogInformation($"Guardando imagen temporal en: {tempFile}");
                    
                    image.Save(tempFile, new SixLabors.ImageSharp.Formats.Tiff.TiffEncoder());
                    
                    try
                    {
                        // Crear instancia de Tesseract con manejo explícito de excepciones
                        _logger.LogInformation("Inicializando motor Tesseract...");
                        using var engine = new TesseractEngine(tessDataPath, "spa", EngineMode.Default);
                        
                        // Configurar parámetros
                        engine.SetVariable("debug_file", "/dev/null");
                        engine.SetVariable("tessedit_char_whitelist", 
                            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,:-_/ ");
                        
                        _logger.LogInformation("Cargando imagen...");
                        using var img = Pix.LoadFromFile(tempFile);
                        _logger.LogInformation($"Imagen cargada: {img.Width}x{img.Height}");
                        
                        _logger.LogInformation("Procesando OCR...");
                        using var page = engine.Process(img);
                        
                        var text = page.GetText();
                        _logger.LogInformation($"OCR completado con éxito. Texto extraído: {text.Substring(0, Math.Min(100, text.Length))}...");
                        
                        return text;
                    }
                    finally
                    {
                        // Asegurar limpieza del archivo temporal
                        try {
                            if (System.IO.File.Exists(tempFile))
                                System.IO.File.Delete(tempFile);
                        }
                        catch (Exception ex) {
                            _logger.LogWarning($"No se pudo eliminar el archivo temporal: {ex.Message}");
                        }
                    }
                });
            }
            catch (DllNotFoundException ex)
            {
                _logger.LogError($"Error: DLL de Tesseract no encontrada: {ex.Message}");
                throw new Exception("La librería nativa de Tesseract no se encuentra instalada en el servidor. Contacte al administrador.", ex);
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError($"Error: Archivo no encontrado: {ex.Message}");
                throw new Exception("Faltan archivos de configuración para el OCR. Contacte al administrador.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en UseTesseractOcr: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
                throw new Exception($"Error procesando OCR: {ex.Message}", ex);
            }
        }

        private OcrResult ExtractRelevantInformation(string text)
        {
            var result = new OcrResult();

            if (string.IsNullOrEmpty(text))
                return result;

            _logger.LogInformation($"Procesando texto: {text}");

            // Hacer una copia original para búsquedas específicas
            string originalText = text;
            // Normalizar texto para mejorar detección
            text = NormalizeText(text);

            // Primera pasada: buscar patrones explícitos con contexto
            ExtractGuiaFromText(text, result);
            ExtractPlacaFromText(text, result);
            ExtractPesoFromText(text, result);
            
            // Segunda pasada: buscar línea por línea para encontrar patrones más difíciles
            var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // Extraer peso si no se encontró en la primera pasada
                if (!result.Peso.HasValue)
                {
                    ExtractPesoFromLine(trimmedLine, result);
                }
                
                // Extraer placa si no se encontró en la primera pasada
                if (string.IsNullOrEmpty(result.Placa))
                {
                    ExtractPlacaFromLine(trimmedLine, result);
                }
                else if (string.IsNullOrEmpty(result.PlacaAlterna))
                {
                    // Buscar placa alternativa
                    ExtractPlacaAlternaFromLine(trimmedLine, result);
                }
                
                // Extraer guía si no se encontró en la primera pasada
                if (string.IsNullOrEmpty(result.Guia))
                {
                    ExtractGuiaFromLine(trimmedLine, result);
                }
            }
            
            // Tercera pasada: buscar patrones aislados si aún faltan datos
            if (string.IsNullOrEmpty(result.Placa))
            {
                ExtractAnyPossiblePlaca(text, result);
            }
            
            if (!result.Peso.HasValue)
            {
                ExtractAnyPossiblePeso(text, result);
            }
            
            if (string.IsNullOrEmpty(result.Guia))
            {
                ExtractAnyPossibleGuia(text, result);
            }

            // Validación final y limpieza
            ValidateAndSanitizeResult(result);

            return result;
        }

        private void ExtractGuiaFromText(string text, OcrResult result)
        {
            // Patrones extendidos para guías con palabras clave asociadas
            var patterns = new[]
            {
                @"(?:guia|guía|remisión|remision|documento|referencia|número|no|numero|num|#)[^\w\d]*(\b[A-Z0-9]{4,15}\b)",
                @"(?:Guía|Guia|Número|No|Remisión|Remision|Documento|Ref).{0,8}[:#\.]?\s*([A-Z0-9]{4,15})",
                @"(?:Guía|Guia|Documento).{0,15}(?:No\.?|número|numero).{0,5}[:#\.]?\s*([A-Z0-9]{4,15})",
                @"(?:No\.?|número|n[uú]mero).{0,10}(?:Guía|Guia|Documento|remision|remisión).{0,8}[:#\.]?\s*([A-Z0-9]{4,15})",
                @"[Gg][Uu][Ii][Aa][\s\W]{1,5}([A-Z0-9]{4,15})",
                @"[Rr]em(?:ision|isión)[\s\W]{1,5}([A-Z0-9]{4,15})",
                @"[Dd]ocumento[\s\W]{1,5}([A-Z0-9]{4,15})",
                @"[Ee]nvio[\s\W]{1,5}([A-Z0-9]{4,15})"
            };
            
            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    result.Guia = CleanAlphanumeric(match.Groups[1].Value.Trim());
                    _logger.LogInformation($"Guía encontrada con patrón: '{pattern}', resultado: '{result.Guia}'");
                    return;
                }
            }
            
            // Búsqueda más agresiva: buscar cualquier código alfanumérico que parezca una guía
            // en líneas que contengan palabras clave
            var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var guiaKeywords = new[] { "guia", "guía", "remision", "remisión", "documento", "envio", "envío", "ref", "referencia" };
            
            foreach (var line in lines)
            {
                string loweredLine = line.ToLower();
                if (guiaKeywords.Any(keyword => loweredLine.Contains(keyword)))
                {
                    // Buscar cualquier secuencia alfanumérica que parezca una guía
                    var alphanumMatches = Regex.Matches(line, @"\b([A-Z0-9]{5,15})\b", RegexOptions.IgnoreCase);
                    foreach (Match match in alphanumMatches)
                    {
                        string possibleGuia = match.Groups[1].Value;
                        if (!Regex.IsMatch(possibleGuia, @"^\d+$")) // Evitar números puros
                        {
                            result.Guia = CleanAlphanumeric(possibleGuia);
                            _logger.LogInformation($"Guía encontrada en línea con palabra clave: '{line}', resultado: '{result.Guia}'");
                            return;
                        }
                    }
                }
            }
        }

        private void ExtractPlacaFromText(string text, OcrResult result)
        {
            // Log del texto completo para diagnóstico
            _logger.LogInformation($"Buscando placas en texto normalizado: {text.Substring(0, Math.Min(200, text.Length))}...");
            
            // Patrones extendidos para placas con palabras clave asociadas
            var patterns = new[]
            {
                @"(?:placa|camión|camion|vehículo|vehiculo|carro|trailer|tracto|mula)[^\w\d]*([A-Z]{2,3}[- ]?\d{3,4})",
                @"(?:Placa|Vehículo|Camión|Tracto).{0,5}[:#]?\s*([A-Z]{2,3}[ -]?\d{3,4})",
                @"[Pp]laca.{0,8}[:#]?\s*([A-Z]{2,3}[ -]?\d{3,4})",
                @"[Vv]eh[ií]culo.{0,8}[:#]?\s*([A-Z]{2,3}[ -]?\d{3,4})",
                @"[Cc]ami[oó]n.{0,8}[:#]?\s*([A-Z]{2,3}[ -]?\d{3,4})",
                @"[Aa]ut[o0]m[oó]vil.{0,8}[:#]?\s*([A-Z]{2,3}[ -]?\d{3,4})",
                @"[Tt]ract[o0][- ]?m[uú]la.{0,8}[:#]?\s*([A-Z]{2,3}[ -]?\d{3,4})",
                @"[Mm]atricula.{0,8}[:#]?\s*([A-Z]{2,3}[ -]?\d{3,4})"
            };
            
            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var placaRaw = match.Groups[1].Value.Trim();
                    _logger.LogInformation($"Posible placa encontrada con patrón: '{pattern}', valor sin limpiar: '{placaRaw}'");
                    result.Placa = CleanPlaca(placaRaw);
                    _logger.LogInformation($"Placa limpiada: '{result.Placa}'");
                    return;
                }
            }
            
            // Búsqueda directa de formato de placas colombianas sin palabras clave
            var directPlacaPatterns = new[] {
                @"\b([A-Z]{3})[ -]?(\d{3})\b",    // AAA-123 formato
                @"\b([A-Z]{3})[ -]?(\d{3,4})\b",  // AAA-1234 formato
                @"\b([A-Z]{2})[ -]?(\d{4})\b"     // AA-1234 formato
            };
            
            foreach (var pattern in directPlacaPatterns)
            {
                var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    var placaRaw = match.Groups[1].Value + match.Groups[2].Value;
                    var possiblePlaca = CleanPlaca(placaRaw);
                    
                    _logger.LogInformation($"Posible placa encontrada directamente: '{placaRaw}', limpiada: '{possiblePlaca}'");
                    
                    // Verificar formato colombiano común
                    if (Regex.IsMatch(possiblePlaca, @"^[A-Z]{2,3}\d{3,4}$"))
                    {
                        result.Placa = possiblePlaca;
                        return;
                    }
                }
            }
        }

        private void ExtractPesoFromText(string text, OcrResult result)
        {
            // Patrones comunes para peso con palabras clave asociadas
            var patterns = new[]
            {
                @"(?:peso|kilogramos|kilos|kg|peso bruto|peso neto)[^\w\d]*(\d+(?:[,\.]\d+)?)",
                @"(?:Peso|Kilos|Kilogramos).{0,5}[:#]?\s*(\d+(?:[.,]\d+)?)",
                @"(\d+(?:[.,]\d+)?)\s*(?:kg|kilo|kilogramo)s?",
                @"[Pp]eso.{0,5}[:#]?\s*(\d+(?:[.,]\d+)?)",
                @"[Kk]ilo(?:s|gramos)?.{0,5}[:#]?\s*(\d+(?:[.,]\d+)?)",
                @"[Tt]oneladas?.{0,5}[:#]?\s*(\d+(?:[.,]\d+)?)"
            };
            
            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    if (double.TryParse(match.Groups[1].Value.Replace(',', '.'), out double peso))
                    {
                        // Si se encontró en toneladas, convertir a kg
                        if (pattern.Contains("onelada"))
                            peso *= 1000;
                        
                        result.Peso = Math.Round(peso, 2);
                        return;
                    }
                }
            }
        }

        private void ExtractPesoFromLine(string line, OcrResult result)
        {
            // Primera búsqueda: patrón específico de peso con kg
            var kgPattern = Regex.Match(line, @"(\d+(?:[.,]\d+)?)\s*(?:kg|kilo|kilogramo)s?", RegexOptions.IgnoreCase);
            if (kgPattern.Success)
            {
                if (double.TryParse(kgPattern.Groups[1].Value.Replace(',', '.'), out double peso))
                {
                    result.Peso = Math.Round(peso, 2);
                    return;
                }
            }
            
            // Segunda búsqueda: línea contiene palabras clave de peso
            if (line.Contains("peso", StringComparison.OrdinalIgnoreCase) || 
                line.Contains("kilo", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("kg", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("bruto", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("neto", StringComparison.OrdinalIgnoreCase))
            {
                // Encontrar cualquier número en la línea
                var numberMatch = Regex.Match(line, @"(\d+(?:[.,]\d+)?)");
                if (numberMatch.Success && double.TryParse(numberMatch.Groups[1].Value.Replace(',', '.'), out double peso))
                {
                    result.Peso = Math.Round(peso, 2);
                    return;
                }
            }
            
            // Tercera búsqueda: toneladas convertidas a kg
            var tonPattern = Regex.Match(line, @"(\d+(?:[.,]\d+)?)\s*(?:ton|tonelada)s?", RegexOptions.IgnoreCase);
            if (tonPattern.Success)
            {
                if (double.TryParse(tonPattern.Groups[1].Value.Replace(',', '.'), out double ton))
                {
                    result.Peso = Math.Round(ton * 1000, 2); // Convertir a kg
                    return;
                }
            }
        }

        private void ExtractPlacaFromLine(string line, OcrResult result)
        {
            // Primera búsqueda: patrón colombiano AAA-123 o AAA123
            var placaFormat1 = Regex.Match(line, @"\b([A-Z]{3})[ -]?(\d{3,4})\b", RegexOptions.IgnoreCase);
            if (placaFormat1.Success)
            {
                result.Placa = CleanPlaca(placaFormat1.Groups[1].Value + placaFormat1.Groups[2].Value);
                return;
            }
            
            // Segunda búsqueda: patrón alterno AA-12345 o AA12345
            var placaFormat2 = Regex.Match(line, @"\b([A-Z]{2})[ -]?(\d{3,5})\b", RegexOptions.IgnoreCase);
            if (placaFormat2.Success && !ContainsVehicleTerms(line)) // Evitar falsos positivos
            {
                result.Placa = CleanPlaca(placaFormat2.Groups[1].Value + placaFormat2.Groups[2].Value);
                return;
            }
            
            // Tercera búsqueda: cualquier texto que parezca una placa (más arriesgado)
            if (line.Contains("placa", StringComparison.OrdinalIgnoreCase) || 
                line.Contains("vehículo", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("vehiculo", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("camion", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("camión", StringComparison.OrdinalIgnoreCase))
            {
                var placaFormatAny = Regex.Match(line, @"([A-Z0-9]{5,7})", RegexOptions.IgnoreCase);
                if (placaFormatAny.Success)
                {
                    var possiblePlaca = CleanPlaca(placaFormatAny.Groups[1].Value);
                    // Verificar que parece una placa válida (letras seguidas de números)
                    if (Regex.IsMatch(possiblePlaca, @"^[A-Z]{2,3}\d{3,4}$"))
                        result.Placa = possiblePlaca;
                }
            }
        }

        private void ExtractPlacaAlternaFromLine(string line, OcrResult result)
        {
            if (result.Placa == null)
                return;
                
            // Buscar otra placa diferente a la principal
            var placaFormats = new[]
            {
                @"\b([A-Z]{3})[ -]?(\d{3,4})\b",
                @"\b([A-Z]{2})[ -]?(\d{3,5})\b"
            };
            
            foreach (var format in placaFormats)
            {
                var match = Regex.Match(line, format, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var groups = match.Groups;
                    var possiblePlaca = CleanPlaca(groups[1].Value + groups[2].Value);
                    if (possiblePlaca != result.Placa)
                    {
                        result.PlacaAlterna = possiblePlaca;
                        return;
                    }
                }
            }
        }

        private void ExtractGuiaFromLine(string line, OcrResult result)
        {
            // Buscamos un alfanumérico que parezca guía y no esté cerca de términos de vehículo
            if (!ContainsVehicleTerms(line))
            {
                // Buscar códigos alfanuméricos de longitud apropiada
                var alphanumMatch = Regex.Match(line, @"\b([A-Z0-9]{5,15})\b", RegexOptions.IgnoreCase);
                if (alphanumMatch.Success)
                {
                    result.Guia = CleanAlphanumeric(alphanumMatch.Groups[1].Value);
                    return;
                }
            }
        }

        private bool ContainsVehicleTerms(string line)
        {
            if (string.IsNullOrEmpty(line))
                return false;

            // Lista de términos relacionados con vehículos para verificar
            string[] vehicleTerms = new string[]
            {
                "placa", "placas", 
                "camión", "camion", "camiones",
                "vehículo", "vehiculo", "vehículos", "vehiculos",
                "carro", "carros", "auto", "autos",
                "trailer", "tráiler", "remolque",
                "automóvil", "automovil",
                "transporte", "transportador",
                "conductor", "chofer", "chófer",
                "tracto", "tractomula",
                "furgón", "furgon", "volqueta"
            };

            // Normalizar el texto para comparación (convertir a minúsculas)
            string normalizedLine = line.ToLower();
            
            // Verificar si algún término aparece en la línea
            foreach (var term in vehicleTerms)
            {
                if (normalizedLine.Contains(term))
                {
                    return true;
                }
            }

            return false;
        }

        private void ExtractAnyPossiblePlaca(string text, OcrResult result)
        {
            _logger.LogInformation("Ejecutando búsqueda agresiva de placas...");
            
            // Búsqueda más agresiva: cualquier patrón que se parezca a una placa
            var placaPossiblePatterns = new[] {
                @"\b([A-Z]{2,3})[^A-Z0-9]*?(\d{3,4})\b",  // Letras seguidas de números con posibles caracteres entre ellos
                @"\b([A-Z][A-Z0-9][A-Z])[^A-Z0-9]*?(\d{3,4})\b",  // Más permisivo, permite un número en el medio
                @"([A-Z]{2,3})[^A-Za-z0-9]{0,3}(\d{3,4})"  // Extremadamente permisivo
            };
            
            foreach (var pattern in placaPossiblePatterns)
            {
                var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    var placaRaw = match.Groups[1].Value + match.Groups[2].Value;
                    var possiblePlaca = CleanPlaca(placaRaw);
                    
                    _logger.LogInformation($"Búsqueda agresiva - posible placa encontrada: '{placaRaw}', limpiada: '{possiblePlaca}'");
                    
                    if (possiblePlaca.Length >= 5 && possiblePlaca.Length <= 7)
                    {
                        // Verificar que tiene al menos 2-3 letras al principio y 3-4 números al final
                        if (Regex.IsMatch(possiblePlaca, @"^[A-Z]{2,3}\d{3,4}$"))
                        {
                            result.Placa = possiblePlaca;
                            _logger.LogInformation($"Placa identificada en búsqueda agresiva: {possiblePlaca}");
                            return;
                        }
                    }
                }
            }
        }

        private void ExtractAnyPossiblePeso(string text, OcrResult result)
        {
            // Última opción: buscar números cercanos a unidades de peso
            var weightPatterns = new[] {
                @"(\d+(?:[.,]\d+)?)(?:\s*|\W*)(?:kg|kilo|kilogramo)",
                @"(?:peso|pesa|pesando)(?:\s*|\W*)(\d+(?:[.,]\d+)?)"
            };
            
            foreach (var pattern in weightPatterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (match.Success && double.TryParse(match.Groups[1].Value.Replace(',', '.'), out double peso))
                {
                    result.Peso = Math.Round(peso, 2);
                    return;
                }
            }
            
            // Si aún no hay peso, buscar cualquier número entre 100 y 50000 (rango típico de kg)
            var numberMatches = Regex.Matches(text, @"\b(\d{3,5}(?:[.,]\d{1,2})?)\b");
            foreach (Match match in numberMatches)
            {
                if (double.TryParse(match.Groups[1].Value.Replace(',', '.'), out double peso))
                {
                    if (peso >= 100 && peso <= 50000)
                    {
                        result.Peso = Math.Round(peso, 2);
                        return;
                    }
                }
            }
        }

        private void ExtractAnyPossibleGuia(string text, OcrResult result)
        {
            // Última opción: buscar cualquier código alfanumérico que no parezca placa
            var alphaNumMatches = Regex.Matches(text, @"\b([A-Z0-9]{6,15})\b", RegexOptions.IgnoreCase);
            
            foreach (Match match in alphaNumMatches)
            {
                var possibleGuia = CleanAlphanumeric(match.Groups[1].Value);
                
                // Evitar confundir con placas o valores numéricos puros
                if (!Regex.IsMatch(possibleGuia, @"^[A-Z]{2,3}\d{3,4}$") && 
                    !Regex.IsMatch(possibleGuia, @"^\d+$"))
                {
                    result.Guia = possibleGuia;
                    return;
                }
            }
        }

        private string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Imprimir los primeros N caracteres para diagnóstico
            _logger.LogInformation($"Texto original (primeros 100 chars): {text.Substring(0, Math.Min(100, text.Length))}");
            
            // Corrección de errores comunes de OCR
            text = text.Replace("l<g", "kg").Replace("l<", "k");
            text = text.Replace("kg.", "kg").Replace("kgs.", "kg").Replace("kgs", "kg");
            
            // Reemplazar caracteres especiales por espacios para mejor tokenización
            text = text.Replace(":", " : ").Replace("-", " - ").Replace("/", " / ");
            
            // Normalizar espacios
            text = Regex.Replace(text, @"\s+", " ");
            
            // Imprimir texto normalizado para diagnóstico
            _logger.LogInformation($"Texto normalizado (primeros 100 chars): {text.Substring(0, Math.Min(100, text.Length))}");
            
            return text;
        }

        private string CleanAlphanumeric(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            // Eliminar todos los caracteres no alfanuméricos
            return Regex.Replace(value, @"[^A-Z0-9]", "", RegexOptions.IgnoreCase);
        }

        private string CleanPlaca(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            // Limpiar y normalizar
            string cleaned = Regex.Replace(value, @"[^A-Z0-9]", "", RegexOptions.IgnoreCase).ToUpper();

            // Correcciones específicas para placas con errores OCR comunes
            cleaned = cleaned.Replace("O", "0"); // En placas vehiculares, O suele ser 0
            cleaned = cleaned.Replace("I", "1"); // En placas vehiculares, I suele ser 1
            cleaned = cleaned.Replace("L", "1"); // En placas vehiculares, L suele ser 1
            cleaned = cleaned.Replace("S", "5"); // A veces S se confunde con 5
            cleaned = cleaned.Replace("Z", "2"); // A veces Z se confunde con 2
            cleaned = cleaned.Replace("B", "8"); // A veces B se confunde con 8
            cleaned = cleaned.Replace("D", "0"); // A veces D se confunde con 0
            cleaned = cleaned.Replace("Q", "0"); // A veces Q se confunde con 0
            cleaned = cleaned.Replace("G", "6"); // A veces G se confunde con 6
            
            return cleaned;
        }

        private void ValidateAndSanitizeResult(OcrResult result)
        {
            // Log pre-validación
            _logger.LogInformation($"Pre-validación: Guía={result.Guia}, Placa={result.Placa}, Peso={result.Peso}");
            
            // Validar y limpiar el peso para valores numéricos razonables
            if (result.Peso.HasValue)
            {
                // Valores imposibles o muy improbables
                if (result.Peso > 100000 || result.Peso < 1)
                {
                    // Si el valor es muy pequeño, podría ser toneladas - multiplicar por 1000
                    if (result.Peso > 0.1 && result.Peso < 100)
                        result.Peso = Math.Round(result.Peso.Value * 1000, 2);
                    else
                        result.Peso = null;
                }
            }

            // Validar placa (formato típico colombiano: 3 letras + 3/4 números o variantes)
            if (!string.IsNullOrEmpty(result.Placa))
            {
                // Hacemos la validación menos estricta - muchas placas pueden tener 
                // formatos ligeramente diferentes según el país o tipo de vehículo
                if (!Regex.IsMatch(result.Placa, @"^[A-Z]{2,3}\d{3,4}$") && 
                    !Regex.IsMatch(result.Placa, @"^[A-Z]\d{4,5}[A-Z]$")) // algunos formatos alternativos
                {
                    _logger.LogInformation($"Placa descartada por validación: {result.Placa}");
                    result.Placa = null;
                }
            }

            // Validar guía (al menos 5 caracteres alfanuméricos)
            if (!string.IsNullOrEmpty(result.Guia))
            {
                if (result.Guia.Length < 5 || result.Guia.Length > 20)
                {
                    _logger.LogInformation($"Guía descartada por longitud: {result.Guia}");
                    result.Guia = null;
                }
                else if (Regex.IsMatch(result.Guia, @"^[A-Z]{2,3}\d{3,4}$"))
                {
                    // Si parece una placa, la descartamos como guía a menos que ya tengamos una placa diferente
                    if (string.IsNullOrEmpty(result.Placa))
                    {
                        _logger.LogInformation($"Moviendo valor de guía a placa porque parece formato de placa: {result.Guia}");
                        result.Placa = result.Guia;
                        result.Guia = null;
                    }
                    else if (result.Guia != result.Placa)
                    {
                        _logger.LogInformation($"Guía tiene formato de placa pero ya tenemos placa diferente, conservando ambas");
                    }
                }
            }
            
            // Log post-validación
            _logger.LogInformation($"Post-validación: Guía={result.Guia}, Placa={result.Placa}, Peso={result.Peso}");
        }
    }

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

    public class OcrSpaceResponse
    {
        public List<ParsedResult>? ParsedResults { get; set; }
    }

    public class ParsedResult
    {
        public string? ParsedText { get; set; }
    }
}