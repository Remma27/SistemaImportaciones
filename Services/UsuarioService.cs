using Sistema_de_Gestion_de_Importaciones.ViewModels;
using API.Models;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using System.Text.Json;

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
                        var usuarios = JsonSerializer.Deserialize<List<Usuario>>(valueElement.GetRawText(), options);
                        return usuarios ?? new List<Usuario>();
                    }
                }
                
                return new List<Usuario>();
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
                
                // Verificar explícitamente que el password_hash esté presente
                if (string.IsNullOrEmpty(usuario.password_hash))
                {
                    // Si el password está vacío, obtener el usuario actual
                    var usuarioActual = await GetByIdAsync(id);
                    if (usuarioActual != null && !string.IsNullOrEmpty(usuarioActual.password_hash))
                    {
                        usuario.password_hash = usuarioActual.password_hash;
                    }
                    else
                    {
                        throw new InvalidOperationException("No se pudo recuperar la contraseña del usuario existente");
                    }
                }
                
                // Log de la petición para diagnóstico
                Console.WriteLine($"[DEBUG] Enviando actualización de usuario con ID {id}");
                Console.WriteLine($"[DEBUG] URL: {_apiUrl}/Edit");
                Console.WriteLine($"[DEBUG] Datos: {JsonSerializer.Serialize(new { usuario.id, usuario.nombre, usuario.email, usuario.activo })}");
                
                // Cambiar la URL para apuntar a la acción Edit
                var response = await _httpClient.PutAsJsonAsync($"{_apiUrl}/Edit", usuario);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[DEBUG] Error response: {errorContent}");
                    throw new Exception($"Error al actualizar el usuario: {response.StatusCode}. Detalle: {errorContent}");
                }
                
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[DEBUG] Response content: {responseContent}");
                
                // Intentar diferentes métodos de deserialización
                try
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    
                    // Primero verificar si hay una propiedad "value"
                    using (JsonDocument doc = JsonDocument.Parse(responseContent))
                    {
                        if (doc.RootElement.TryGetProperty("value", out JsonElement valueElement))
                        {
                            var result = JsonSerializer.Deserialize<Usuario>(valueElement.GetRawText(), options);
                            if (result != null)
                            {
                                return result;
                            }
                        }
                    }
                    
                    // Si no tiene "value", intentar deserializar directamente
                    var resultDirect = JsonSerializer.Deserialize<Usuario>(responseContent, options);
                    if (resultDirect != null)
                    {
                        return resultDirect;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DEBUG] Error deserializando respuesta: {ex.Message}");
                }
                
                // Si todo lo demás falla, simplemente devolvemos el usuario que se envió
                return usuario;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[DEBUG] HttpRequestException: {ex.Message}");
                throw new Exception($"Error al actualizar el usuario: {ex.Message}", ex);
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
                var model = new AsignarRolViewModel 
                { 
                    UsuarioId = usuarioId, 
                    RolId = rolId 
                };
                
                var response = await _httpClient.PostAsJsonAsync($"{_apiUrl}/AsignarRol", model);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return OperationResult.CreateFailure($"Error al asignar rol: {response.StatusCode} - {errorContent}");
                }
                
                return OperationResult.CreateSuccess(true);
            }
            catch (Exception ex)
            {
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
                        if (dynamicObj != null && dynamicObj.value != null)
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