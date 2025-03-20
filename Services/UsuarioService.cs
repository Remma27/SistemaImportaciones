using Sistema_de_Gestion_de_Importaciones.ViewModels;
using API.Models;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;

namespace SistemaDeGestionDeImportaciones.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;

        public UsuarioService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            var baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5079";
            _apiUrl = baseUrl.EndsWith("/") ? $"{baseUrl}api/Usuario" : $"{baseUrl}/api/Usuario";
            Console.WriteLine($"[Debug] _apiUrl: {_apiUrl}");
        }

        public async Task<IEnumerable<Usuario>> GetAllAsync()
        {
            try
            {
                // Añadir la acción específica a la URL
                var response = await _httpClient.GetAsync($"{_apiUrl}/GetAll");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error al obtener usuarios: {response.StatusCode}. Detalle: {errorContent}");
                }
                
                var jsonString = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[DEBUG] GetAllAsync response: {jsonString.Substring(0, Math.Min(100, jsonString.Length))}...");
                
                // Deserializar con opciones para manejar referencias circulares
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReferenceHandler = ReferenceHandler.Preserve
                };
                
                try
                {
                    // Intento 1: Deserializar directamente a lista de usuarios
                    return JsonSerializer.Deserialize<List<Usuario>>(jsonString, options) ?? new List<Usuario>();
                }
                catch (Exception ex1)
                {
                    Console.WriteLine($"[DEBUG] Error en deserialización directa: {ex1.Message}");
                    
                    try
                    {
                        // Intento 2: Verificar si hay una propiedad 'value'
                        using (var doc = JsonDocument.Parse(jsonString))
                        {
                            if (doc.RootElement.TryGetProperty("value", out var valueElement))
                            {
                                return JsonSerializer.Deserialize<List<Usuario>>(valueElement.GetRawText(), options) ?? new List<Usuario>();
                            }
                            
                            // Intento 3: Verificar si la raíz es un array
                            if (doc.RootElement.ValueKind == JsonValueKind.Array)
                            {
                                return JsonSerializer.Deserialize<List<Usuario>>(jsonString, options) ?? new List<Usuario>();
                            }
                        }
                    }
                    catch (Exception ex2)
                    {
                        Console.WriteLine($"[DEBUG] Error en deserialización con JsonDocument: {ex2.Message}");
                    }
                    
                    // Intento de último recurso usando Newtonsoft.Json que es más tolerante
                    try
                    {
                        var usuarios = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Usuario>>(jsonString);
                        return usuarios ?? new List<Usuario>();
                    }
                    catch (Exception ex3)
                    {
                        Console.WriteLine($"[DEBUG] Error en deserialización con Newtonsoft: {ex3.Message}");
                        throw new Exception($"No se pudieron deserializar los usuarios: {ex1.Message}", ex1);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al obtener los usuarios: {ex.Message}", ex);
            }
        }

        public async Task<Usuario> GetByIdAsync(int id)
        {
            try
            {
                // Añadir la acción específica a la URL
                var response = await _httpClient.GetAsync($"{_apiUrl}/Get?id={id}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error al obtener el usuario {id}: {response.StatusCode}. Detalle: {errorContent}");
                }
                
                var jsonString = await response.Content.ReadAsStringAsync();
                
                // Deserializar manualmente con opciones específicas
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                // Extraer datos del objeto de respuesta
                using (var doc = JsonDocument.Parse(jsonString))
                {
                    if (doc.RootElement.TryGetProperty("value", out var valueElement))
                    {
                        var usuario = JsonSerializer.Deserialize<Usuario>(valueElement.GetRawText(), options);
                        if (usuario == null)
                            throw new KeyNotFoundException($"No se encontró el usuario con ID {id}");
                        
                        return usuario;
                    }
                }
                
                throw new KeyNotFoundException($"No se encontró el usuario con ID {id}");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al obtener el usuario {id}: {ex.Message}", ex);
            }
        }

        public async Task<Usuario> CreateAsync(Usuario usuario)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(_apiUrl, usuario);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<Usuario>();
                return result ?? throw new InvalidOperationException("Error al crear el usuario");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al crear el usuario: {ex.Message}", ex);
            }
        }

        public async Task<Usuario> UpdateAsync(int id, Usuario usuario)
        {
            try
            {
                // Verificar que el ID sea consistente
                usuario.id = id;
                
                Console.WriteLine($"[DEBUG-CRÍTICO] Actualizando usuario {id}: {usuario.nombre}, {usuario.email}, Activo: {usuario.activo}");
                
                // Validar datos obligatorios
                if (string.IsNullOrWhiteSpace(usuario.nombre) || string.IsNullOrWhiteSpace(usuario.email))
                {
                    throw new ArgumentException("El nombre y email son obligatorios");
                }

                // Crear objeto simplificado para la actualización que no incluye password_hash
                var actualizacionUsuario = new
                {
                    id = usuario.id,
                    nombre = usuario.nombre,
                    email = usuario.email,
                    activo = usuario.activo,
                    rol_id = usuario.rol_id
                    // No incluimos password_hash intencionalmente
                };
                
                // Configurar opciones de serialización
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                
                // Serializar los datos de actualización
                var jsonContent = JsonSerializer.Serialize(actualizacionUsuario, jsonOptions);
                Console.WriteLine($"[DEBUG-CRÍTICO] JSON a enviar: {jsonContent}");
                
                // Crear contenido HTTP
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                // Realizar la petición
                var response = await _httpClient.PutAsync($"{_apiUrl}/Edit", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[DEBUG-CRÍTICO] Error response: {errorContent}");
                    throw new Exception($"Error al actualizar el usuario: {response.StatusCode}. Detalle: {errorContent}");
                }
                
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[DEBUG-CRÍTICO] Respuesta exitosa: {responseContent}");
                
                // Devolver el usuario actualizado
                return usuario;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG-CRÍTICO] Error general en UpdateAsync: {ex.Message}");
                Console.WriteLine($"[DEBUG-CRÍTICO] StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                // Corrigiendo la URL para usar el endpoint correcto
                var response = await _httpClient.DeleteAsync($"{_apiUrl}/Delete?id={id}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error al eliminar el usuario: {response.StatusCode}. Detalle: {errorContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al eliminar el usuario: {ex.Message}", ex);
            }
        }

        public async Task<OperationResult> RegistrarUsuarioAsync(RegistroViewModel model)
        {
            try
            {
                var usuarioToCreate = new Usuario
                {
                    id = 0,
                    nombre = model.Nombre,
                    email = model.Email,
                    password_hash = model.Password,
                    fecha_creacion = DateTime.Now,
                    activo = true
                };

                var response = await _httpClient.PostAsJsonAsync($"{_apiUrl}/Registrar", usuarioToCreate);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[ERROR] Status: {response.StatusCode}, Content: {errorContent}");
                    return OperationResult.CreateFailure($"Error al registrar el usuario: {response.StatusCode} - {errorContent}");
                }

                var usuario = await response.Content.ReadFromJsonAsync<Usuario>();
                if (usuario == null)
                {
                    return OperationResult.CreateFailure("Error al registrar el usuario");
                }
                return OperationResult.CreateSuccess(usuario);
            }
            catch (HttpRequestException ex)
            {
                return OperationResult.CreateFailure($"Error al registrar el usuario: {ex.Message}");
            }
        }

        public async Task<OperationResult> IniciarSesionAsync(LoginViewModel model)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.PostAsJsonAsync($"{_apiUrl}/IniciarSesion", model);

                var jsonString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    try
                    {
                        var errorObj = JsonSerializer.Deserialize<JsonElement>(jsonString);
                        var errorMessage = errorObj.TryGetProperty("message", out var msgElement)
                            ? msgElement.GetString() ?? "Error desconocido en la API"
                            : "Error desconocido en la API";

                        // Log más detallado para el error de inicio de sesión
                        Console.WriteLine($"[ERROR] Login fallido - Código: {response.StatusCode}, Mensaje: {errorMessage}");
                        
                        // Verificar si es un mensaje específico de cuenta desactivada
                        if (errorMessage.Contains("cuenta está desactivada") || errorMessage.Contains("inactivo"))
                        {
                            return OperationResult.CreateFailure(errorMessage);
                        }

                        return OperationResult.CreateFailure(errorMessage);
                    }
                    catch
                    {
                        return OperationResult.CreateFailure($"Error al iniciar sesión: {response.StatusCode}");
                    }
                }

                try
                {
                    var responseObj = JsonSerializer.Deserialize<JsonElement>(jsonString);

                    if (responseObj.TryGetProperty("user", out var userElement))
                    {
                        var usuario = JsonSerializer.Deserialize<Usuario>(userElement.GetRawText());
                        if (usuario == null)
                        {
                            return OperationResult.CreateFailure("No se pudo deserializar los datos del usuario");
                        }
                        return OperationResult.CreateSuccess(usuario);
                    }
                    else
                    {
                        return OperationResult.CreateFailure("Formato de respuesta inválido");
                    }
                }
                catch (Exception ex)
                {
                    return OperationResult.CreateFailure($"Error al procesar respuesta: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                return OperationResult.CreateFailure($"Error en la conexión: {ex.Message}");
            }
        }

        // Método para asignar un rol a un usuario
        public async Task<OperationResult> AsignarRolAsync(int usuarioId, int rolId)
        {
            try
            {
                // Añadir logging detallado para diagnóstico
                Console.WriteLine($"[DEBUG] Iniciando AsignarRolAsync - UsuarioId: {usuarioId}, RolId: {rolId}");
                
                // Validar los datos de entrada
                if (usuarioId <= 0)
                    return OperationResult.CreateFailure("ID de usuario inválido");
                
                if (rolId <= 0)
                    return OperationResult.CreateFailure("ID de rol inválido");
                    
                var model = new AsignarRolViewModel 
                { 
                    UsuarioId = usuarioId, 
                    RolId = rolId 
                };
                
                // Mostrar URL completa a la que se enviará la solicitud
                string requestUrl = $"{_apiUrl}/AsignarRol";
                Console.WriteLine($"[DEBUG] URL para asignar rol: {requestUrl}");
                Console.WriteLine($"[DEBUG] Datos a enviar: UsuarioId={usuarioId}, RolId={rolId}");
                
                // Mejorar el manejo de errores HTTP
                var jsonContent = JsonSerializer.Serialize(model);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync(requestUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"[DEBUG] Status Code: {response.StatusCode}");
                Console.WriteLine($"[DEBUG] Respuesta: {responseContent}");
                
                if (!response.IsSuccessStatusCode)
                {
                    return OperationResult.CreateFailure($"Error al asignar rol: {response.StatusCode} - {responseContent}");
                }
                
                // Verificar el formato de la respuesta
                try
                {
                    var responseObj = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    if (responseObj.TryGetProperty("message", out var msgElement))
                    {
                        string message = msgElement.GetString() ?? "Rol asignado correctamente";
                        Console.WriteLine($"[DEBUG] Mensaje de éxito: {message}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DEBUG] Error al procesar respuesta (pero la operación fue exitosa): {ex.Message}");
                }
                
                return OperationResult.CreateSuccess(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Excepción en AsignarRolAsync: {ex.Message}");
                Console.WriteLine($"[DEBUG] Stack Trace: {ex.StackTrace}");
                return OperationResult.CreateFailure($"Error de conexión: {ex.Message}");
            }
        }

        // Método para cambiar la contraseña de un usuario
        public async Task<OperationResult> CambiarPasswordAsync(int usuarioId, string newPassword)
        {
            try
            {
                var usuario = await GetByIdAsync(usuarioId);
                if (usuario == null)
                    return OperationResult.CreateFailure("Usuario no encontrado");
                
                // Asignar nueva contraseña y mantener los demás datos
                usuario.password_hash = newPassword;
                
                var response = await _httpClient.PutAsJsonAsync($"{_apiUrl}/Edit", usuario);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return OperationResult.CreateFailure($"Error al cambiar contraseña: {response.StatusCode} - {errorContent}");
                }
                
                return OperationResult.CreateSuccess(true);
            }
            catch (Exception ex)
            {
                return OperationResult.CreateFailure($"Error de conexión: {ex.Message}");
            }
        }

        // Método específico para cambiar el estado activo de un usuario sin necesitar la contraseña
        public async Task<OperationResult> ToggleActivoAsync(int usuarioId, bool nuevoEstado)
        {
            try
            {
                // URL específica para cambiar solo el estado activo
                string url = $"{_apiUrl}/ToggleActivo?id={usuarioId}&activo={nuevoEstado}";
                
                var response = await _httpClient.PostAsync(url, null);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return OperationResult.CreateFailure($"Error al cambiar estado del usuario: {response.StatusCode} - {errorContent}");
                }
                
                return OperationResult.CreateSuccess(true);
            }
            catch (Exception ex)
            {
                return OperationResult.CreateFailure($"Error de conexión: {ex.Message}");
            }
        }

        // Método para obtener todos los roles disponibles
        public async Task<List<Rol>> GetRolesAsync()
        {
            try
            {
                // URL correcta para obtener roles
                string baseUrl = _apiUrl.Substring(0, _apiUrl.LastIndexOf("/api/") + 5);
                string requestUrl = $"{baseUrl}Rol/GetAll";
                Console.WriteLine($"[ROLES] Solicitando roles desde: {requestUrl}");
                
                var response = await _httpClient.GetAsync(requestUrl);
                Console.WriteLine($"[ROLES] Response status: {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[ROLES] Error: HTTP {response.StatusCode}");
                    return GetRolesRespaldo();
                }
                
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[ROLES] Respuesta recibida ({content.Length} bytes)");
                
                try
                {
                    // Intentar deserializar directamente la respuesta
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    };
                    
                    // Primero verificar si el contenido es un objeto con propiedad "value"
                    using (JsonDocument doc = JsonDocument.Parse(content))
                    {
                        try
                        {
                            // Si la respuesta tiene una propiedad "value", extraer esa parte
                            if (doc.RootElement.TryGetProperty("value", out var valueElement))
                            {
                                var roles = JsonSerializer.Deserialize<List<Rol>>(valueElement.GetRawText(), options);
                                Console.WriteLine($"[ROLES] Roles extraídos de propiedad 'value': {roles?.Count ?? 0}");
                                return roles ?? new List<Rol>();
                            }
                            
                            // Si es un array directamente
                            if (doc.RootElement.ValueKind == JsonValueKind.Array)
                            {
                                var roles = JsonSerializer.Deserialize<List<Rol>>(content, options);
                                Console.WriteLine($"[ROLES] Roles obtenidos de array directo: {roles?.Count ?? 0}");
                                return roles ?? new List<Rol>();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ROLES] Error al deserializar JSON: {ex.Message}");
                        }
                    }
                    
                    // Si todo falla, intentar un último método
                    try
                    {
                        var responseObj = await response.Content.ReadFromJsonAsync<object>(options);
                        var jsonString = JsonSerializer.Serialize(responseObj);
                        Console.WriteLine($"[ROLES] Intentando con lectura genérica: {jsonString.Substring(0, Math.Min(100, jsonString.Length))}...");
                        
                        // Intentar extraer usando dynamic
                        var roles = new List<Rol>();
                        var dynamicObj = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(content);
                        if (dynamicObj?.value != null)
                        {
                            foreach (var item in dynamicObj.value)
                            {
                                roles.Add(new Rol
                                {
                                    id = item.id,
                                    nombre = item.nombre,
                                    descripcion = item.descripcion
                                });
                            }
                            Console.WriteLine($"[ROLES] Roles extraídos con Newtonsoft: {roles.Count}");
                            return roles;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ROLES] Error con método alternativo: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ROLES] Error general: {ex.Message}");
                }
                
                return GetRolesRespaldo();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ROLES] Excepción no controlada: {ex.Message}");
                return GetRolesRespaldo();
            }
        }

        private List<Rol> GetRolesRespaldo()
        {
            Console.WriteLine("[DEBUG] Creando roles locales de respaldo");
            return new List<Rol>
            {
                new Rol { id = 1, nombre = "Administrador", descripcion = "Acceso completo al sistema" },
                new Rol { id = 2, nombre = "Operador", descripcion = "Acceso a operaciones básicas" },
                new Rol { id = 3, nombre = "Usuario", descripcion = "Acceso limitado solo lectura" }
            };
        }

        // Clases anidadas para deserializar respuestas
        private class ApiResponse
        {
            public required List<Usuario> Value { get; set; }
        }

        private class ApiSingleResponse
        {
            public required Usuario Value { get; set; }
        }
    }
}