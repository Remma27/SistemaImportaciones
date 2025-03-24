using Microsoft.Extensions.Configuration;
using API.Models;
using Sistema_de_Gestion_de_Importaciones.ViewModels;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using Sistema_de_Gestion_de_Importaciones.Models;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SistemaDeGestionDeImportaciones.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;
        private readonly ILogger<UsuarioService>? _logger;

        public UsuarioService(HttpClient httpClient, IConfiguration configuration, ILogger<UsuarioService>? logger = null)
        {
            _httpClient = httpClient;
            _apiUrl = configuration["ApiSettings:BaseUrl"] + "/api/Usuario";
            _logger = logger;
        }

        public async Task<IEnumerable<Usuario>> GetAllAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_apiUrl}/GetAll");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError("Error fetching users: {StatusCode}, {Content}", response.StatusCode, errorContent);
                    throw new Exception($"Error al obtener usuarios: {response.StatusCode}. Detalle: {errorContent}");
                }
                
                var jsonString = await response.Content.ReadAsStringAsync();
                _logger?.LogDebug("GetAllAsync response: {Response}", 
                    jsonString.Length > 100 ? jsonString.Substring(0, 100) + "..." : jsonString);
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReferenceHandler = ReferenceHandler.Preserve
                };
                
                using JsonDocument document = JsonDocument.Parse(jsonString);
                var root = document.RootElement;
                
                _logger?.LogDebug("JSON Root Kind: {RootKind}", root.ValueKind);
                
                if (root.ValueKind == JsonValueKind.Array)
                {
                    _logger?.LogInformation("Detected direct array format");
                    var usuarios = JsonSerializer.Deserialize<List<Usuario>>(jsonString, options);
                    _logger?.LogInformation("Deserialized {Count} users", usuarios?.Count ?? 0);
                    return usuarios ?? new List<Usuario>();
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("value", out JsonElement valueElement))
                    {
                        _logger?.LogInformation("Detected 'value' property format");
                        var usuariosJson = valueElement.GetRawText();
                        var usuarios = JsonSerializer.Deserialize<List<Usuario>>(usuariosJson, options);
                        return usuarios ?? new List<Usuario>();
                    }
                    
                    try
                    {
                        _logger?.LogInformation("Trying Newtonsoft.Json deserialization");
                        var settings = new Newtonsoft.Json.JsonSerializerSettings
                        {
                            ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                        };
                        var usuarios = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Usuario>>(jsonString, settings);
                        if (usuarios != null)
                        {
                            _logger?.LogInformation("Deserialized {Count} users with Newtonsoft", usuarios.Count);
                            return usuarios;
                        }
                    }
                    catch (Exception ex3)
                    {
                        _logger?.LogError(ex3, "Newtonsoft.Json deserialization error");
                    }
                }
                
                _logger?.LogWarning("Could not deserialize response in any known format");
                return new List<Usuario>();
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "HTTP request error");
                throw new Exception($"Error al obtener los usuarios: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error in GetAllAsync");
                throw new Exception($"Error inesperado al obtener usuarios: {ex.Message}", ex);
            }
        }

        public async Task<Usuario> GetByIdAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_apiUrl}/Get?id={id}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError("Error fetching user {Id}: {StatusCode}, {Content}", id, response.StatusCode, errorContent);
                    throw new Exception($"Error al obtener usuario: {response.StatusCode}. Detalle: {errorContent}");
                }
                
                var content = await response.Content.ReadAsStringAsync();
                _logger?.LogDebug("GetByIdAsync response: {Response}", 
                    content.Length > 100 ? content.Substring(0, 100) + "..." : content);
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReferenceHandler = ReferenceHandler.Preserve
                };
                
                using JsonDocument document = JsonDocument.Parse(content);
                var root = document.RootElement;
                
                Usuario? usuario = null;
                
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("value", out JsonElement valueElement))
                    {
                        usuario = JsonSerializer.Deserialize<Usuario>(valueElement.GetRawText(), options);
                    }
                    else
                    {
                        usuario = JsonSerializer.Deserialize<Usuario>(content, options);
                    }
                }
                
                if (usuario == null)
                {
                    try
                    {
                        usuario = Newtonsoft.Json.JsonConvert.DeserializeObject<Usuario>(content);
                    }
                    catch
                    {
                    }
                }
                
                return usuario ?? throw new KeyNotFoundException($"No se encontró el usuario con ID {id}");
            }
            catch (Exception ex) when (ex is not KeyNotFoundException)
            {
                _logger?.LogError(ex, "Error getting user {Id}", id);
                throw new Exception($"Error al obtener el usuario: {ex.Message}", ex);
            }
        }

        public async Task<Usuario> CreateAsync(Usuario usuario)
        {
            try
            {
                _logger?.LogInformation("Creating user: {Email}", usuario.email);
                
                var response = await _httpClient.PostAsJsonAsync($"{_apiUrl}/Create", usuario);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError("Error creating user: {StatusCode}, {Content}", response.StatusCode, errorContent);
                    throw new Exception($"Error al crear usuario: {response.StatusCode}. Detalle: {errorContent}");
                }
                
                var content = await response.Content.ReadAsStringAsync();
                _logger?.LogDebug("CreateAsync response: {Response}", 
                    content.Length > 100 ? content.Substring(0, 100) + "..." : content);
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReferenceHandler = ReferenceHandler.Preserve
                };

                try
                {
                    using var document = JsonDocument.Parse(content);
                    
                    if (document.RootElement.TryGetProperty("value", out var valueElement))
                    {
                        return JsonSerializer.Deserialize<Usuario>(valueElement.GetRawText(), options) ?? usuario;
                    }
                    
                    return JsonSerializer.Deserialize<Usuario>(content, options) ?? usuario;
                }
                catch (JsonException)
                {
                    try
                    {
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<Usuario>(content) ?? usuario;
                    }
                    catch
                    {
                        return usuario;
                    }
                }
            }
            catch (Exception ex) when (ex is not JsonException)
            {
                _logger?.LogError(ex, "Error creating user");
                throw new Exception($"Error al crear el usuario: {ex.Message}", ex);
            }
        }

        public async Task<Usuario> UpdateAsync(int id, Usuario usuario)
        {
            try
            {
                _logger?.LogInformation("Updating user: {Id}", id);
                
                var response = await _httpClient.PutAsJsonAsync($"{_apiUrl}/Edit", usuario);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError("Error updating user {Id}: {StatusCode}, {Content}", id, response.StatusCode, errorContent);
                    throw new Exception($"Error al actualizar usuario: {response.StatusCode}. Detalle: {errorContent}");
                }
                
                var content = await response.Content.ReadAsStringAsync();
                _logger?.LogDebug("UpdateAsync response: {Response}", 
                    content.Length > 100 ? content.Substring(0, 100) + "..." : content);
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReferenceHandler = ReferenceHandler.Preserve
                };

                try
                {
                    using var document = JsonDocument.Parse(content);
                    
                    if (document.RootElement.TryGetProperty("value", out var valueElement))
                    {
                        return JsonSerializer.Deserialize<Usuario>(valueElement.GetRawText(), options) ?? usuario;
                    }
                    
                    return JsonSerializer.Deserialize<Usuario>(content, options) ?? usuario;
                }
                catch (JsonException)
                {
                    try
                    {
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<Usuario>(content) ?? usuario;
                    }
                    catch
                    {
                        return usuario;
                    }
                }
            }
            catch (Exception ex) when (ex is not JsonException)
            {
                _logger?.LogError(ex, "Error updating user {Id}", id);
                throw new Exception($"Error al actualizar el usuario: {ex.Message}", ex);
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                _logger?.LogInformation("Deleting user: {Id}", id);
                
                var response = await _httpClient.DeleteAsync($"{_apiUrl}/Delete?id={id}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError("Error deleting user {Id}: {StatusCode}, {Content}", id, response.StatusCode, errorContent);
                    throw new Exception($"Error al eliminar usuario: {response.StatusCode}. Detalle: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting user {Id}", id);
                throw new Exception($"Error al eliminar el usuario: {ex.Message}", ex);
            }
        }

        public async Task<OperationResult> RegistrarUsuarioAsync(RegistroViewModel model)
        {
            try
            {
                _logger?.LogInformation("Registering new user: {Email}", model.Email);
                
                var usuario = new Usuario
                {
                    nombre = model.Nombre,
                    email = model.Email,
                    password_hash = model.Password,
                    activo = true,
                    fecha_creacion = DateTime.Now
                };
                
                var response = await _httpClient.PostAsJsonAsync($"{_apiUrl}/Registrar", usuario);
                var content = await response.Content.ReadAsStringAsync();
                
                _logger?.LogDebug("Registration response: {Response}", 
                    content.Length > 100 ? content.Substring(0, 100) + "..." : content);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogError("Registration error: {StatusCode}, {Content}", response.StatusCode, content);
                    
                    if (content.Contains("ya está registrado"))
                    {
                        return new OperationResult { Success = false, message = "El correo electrónico ya está registrado" };
                    }
                    
                    return new OperationResult { Success = false, message = $"Error de registro: {content}" };
                }
                
                return new OperationResult { Success = true, message = "Usuario registrado correctamente" };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during user registration");
                return new OperationResult { Success = false, message = $"Error al registrar usuario: {ex.Message}" };
            }
        }

        public async Task<OperationResult> IniciarSesionAsync(LoginViewModel model)
        {
            try
            {
                _logger?.LogInformation("Initiating login for user: {Email}", model.Email);
                
                var response = await _httpClient.PostAsJsonAsync($"{_apiUrl}/IniciarSesion", model);
                var content = await response.Content.ReadAsStringAsync();
                
                _logger?.LogDebug("Login response: {Response}", 
                    content.Length > 100 ? content.Substring(0, 100) + "..." : content);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogWarning("Login failed: {StatusCode}, {Content}", response.StatusCode, content);
                    
                    if (content.Contains("Credenciales inválidas"))
                    {
                        return new OperationResult { Success = false, Message = "Credenciales inválidas" };
                    }
                    
                    if (content.Contains("cuenta está desactivada"))
                    {
                        return new OperationResult { Success = false, Message = "Su cuenta está desactivada. Contacte al administrador." };
                    }
                    
                    return new OperationResult { Success = false, Message = $"Error de inicio de sesión: {content}" };
                }
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReferenceHandler = ReferenceHandler.Preserve
                };
                
                try
                {
                    using var document = JsonDocument.Parse(content);
                    var root = document.RootElement;
                    
                    string? userRole = null;
                    int userId = 0;
                    string? userName = null;
                    
                    if (root.TryGetProperty("user", out var userElement))
                    {
                        if (userElement.TryGetProperty("id", out var idElement))
                        {
                            userId = idElement.GetInt32();
                        }
                        
                        if (userElement.TryGetProperty("nombre", out var nameElement))
                        {
                            userName = nameElement.GetString();
                        }
                        
                        if (userElement.TryGetProperty("rol", out var rolElement))
                        {
                            userRole = rolElement.GetString();
                        }
                    }
                    
                    return new OperationResult 
                    { 
                        Success = true, 
                        Message = "Inicio de sesión exitoso",
                        Data = new Dictionary<string, object?>
                        {
                            ["UserId"] = userId,
                            ["UserName"] = userName,
                            ["UserRole"] = userRole
                        }
                    };
                }
                catch (JsonException jsonEx)
                {
                    _logger?.LogError(jsonEx, "Error parsing login response");
                    return new OperationResult { Success = true, Message = "Inicio de sesión exitoso, pero hubo un problema procesando la respuesta." };
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during login");
                return new OperationResult { Success = false, Message = $"Error al iniciar sesión: {ex.Message}" };
            }
        }

        public async Task<OperationResult> AsignarRolAsync(int usuarioId, int rolId)
        {
            try
            {
                _logger?.LogInformation("Assigning role {RolId} to user {UserId}", rolId, usuarioId);
                
                var model = new 
                {
                    UsuarioId = usuarioId, 
                    RolId = rolId
                };
                
                var response = await _httpClient.PostAsJsonAsync($"{_apiUrl}/AsignarRol", model);
                var content = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogError("Role assignment error: {StatusCode}, {Content}", response.StatusCode, content);
                    return new OperationResult { Success = false, Message = $"Error al asignar rol: {content}" };
                }
                
                return new OperationResult { Success = true, Message = "Rol asignado correctamente" };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error assigning role to user {UserId}", usuarioId);
                return new OperationResult { Success = false, Message = $"Error al asignar rol: {ex.Message}" };
            }
        }

        public async Task<OperationResult> CambiarPasswordAsync(int usuarioId, string newPassword)
        {
            try
            {
                _logger?.LogInformation("Changing password for user {UserId}", usuarioId);
                
                if (string.IsNullOrEmpty(newPassword))
                {
                    _logger?.LogWarning("Attempted to change password with empty password for user {UserId}", usuarioId);
                    return new OperationResult { Success = false, Message = "La contraseña no puede estar vacía" };
                }
                
                var model = new 
                {
                    UsuarioId = usuarioId, 
                    NewPassword = newPassword
                };
                
                _logger?.LogDebug("Sending password change request for user {UserId} to API", usuarioId);
                
                string endpoint = $"{_apiUrl}/CambiarPassword";
                _logger?.LogInformation("Calling API endpoint: {Endpoint}", endpoint);
                
                try
                {
                    string requestJson = System.Text.Json.JsonSerializer.Serialize(model);
                    _logger?.LogDebug("Request payload: {RequestJson}", requestJson);
                }
                catch (Exception logEx)
                {
                    _logger?.LogWarning("Could not serialize request for logging: {ErrorMessage}", logEx.Message);
                }
                HttpResponseMessage response;
                try
                {
                    response = await _httpClient.PostAsJsonAsync(endpoint, model);
                }
                catch (Exception httpEx)
                {
                    _logger?.LogError(httpEx, "HTTP exception when calling password change endpoint");
                    return new OperationResult { 
                        Success = false, 
                        Message = $"Error de comunicación con el servidor: {httpEx.Message}" 
                    };
                }
                
                string content;
                try
                {
                    content = await response.Content.ReadAsStringAsync();
                    _logger?.LogDebug("Password change response status: {StatusCode}, content: {Content}", 
                        response.StatusCode, 
                        content.Length > 100 ? content.Substring(0, 100) + "..." : content);
                }
                catch (Exception readEx)
                {
                    _logger?.LogError(readEx, "Error reading response content");
                    content = $"Error al leer respuesta: {readEx.Message}";
                }
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogError("Password change error: {StatusCode}, {Content}", response.StatusCode, content);
                    string errorMessage = $"Error al cambiar contraseña (Código: {(int)response.StatusCode})";
                    
                    if (!string.IsNullOrEmpty(content))
                    {
                        try
                        {
                            using var document = JsonDocument.Parse(content);
                            if (document.RootElement.TryGetProperty("message", out var messageElement))
                            {
                                errorMessage = messageElement.GetString() ?? errorMessage;
                            }
                        }
                        catch (Exception jsonEx)
                        {
                            _logger?.LogWarning(jsonEx, "Failed to parse error response as JSON");
                            errorMessage += ": " + content;
                        }
                    }
                    
                    return new OperationResult { Success = false, Message = errorMessage };
                }
                
                return new OperationResult { Success = true, Message = "Contraseña cambiada correctamente" };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error changing password for user {UserId}", usuarioId);
                return new OperationResult { Success = false, Message = $"Error al cambiar contraseña: {ex.Message}" };
            }
        }

        public async Task<List<Rol>> GetRolesAsync()
        {
            try
            {
                _logger?.LogInformation("Getting roles list");
                
                var response = await _httpClient.GetAsync("api/Rol/GetAll");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError("Error fetching roles: {StatusCode}, {Content}", response.StatusCode, errorContent);
                    throw new Exception($"Error al obtener roles: {response.StatusCode}");
                }
                
                var content = await response.Content.ReadAsStringAsync();
                
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    using var document = JsonDocument.Parse(content);
                    var root = document.RootElement;
                    
                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        var roles = JsonSerializer.Deserialize<List<Rol>>(content, options);
                        return roles ?? new List<Rol>();
                    }
                    
                    if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("value", out var valueElement))
                    {
                        var roles = JsonSerializer.Deserialize<List<Rol>>(valueElement.GetRawText(), options);
                        return roles ?? new List<Rol>();
                    }
                    
                    var rolesFromNewtonsoft = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Rol>>(content);
                    if (rolesFromNewtonsoft != null)
                    {
                        return rolesFromNewtonsoft;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error deserializing roles");
                }
                
                return new List<Rol>();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting roles");
                throw new Exception($"Error al obtener roles: {ex.Message}", ex);
            }
        }

        public async Task<OperationResult> ToggleActivoAsync(int usuarioId, bool nuevoEstado)
        {
            try
            {
                _logger?.LogInformation("Toggling active status to {Estado} for user {UserId}", nuevoEstado, usuarioId);
                
                var response = await _httpClient.PostAsJsonAsync($"{_apiUrl}/ToggleActivo?id={usuarioId}&activo={nuevoEstado}", new { });
                var content = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogError("Toggle active error: {StatusCode}, {Content}", response.StatusCode, content);
                    return new OperationResult { Success = false, Message = $"Error al cambiar estado: {content}" };
                }
                
                var estadoText = nuevoEstado ? "activado" : "desactivado";
                return new OperationResult { Success = true, Message = $"Usuario {estadoText} correctamente" };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error toggling active status for user {UserId}", usuarioId);
                return new OperationResult { Success = false, Message = $"Error al cambiar estado: {ex.Message}" };
            }
        }
    }
}