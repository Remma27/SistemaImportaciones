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
                var result = await _httpClient.GetFromJsonAsync<IEnumerable<Usuario>>(_apiUrl);
                return result ?? Enumerable.Empty<Usuario>();
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
                var result = await _httpClient.GetFromJsonAsync<Usuario>($"{_apiUrl}/{id}");
                return result ?? throw new KeyNotFoundException($"No se encontró el usuario con ID {id}");
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
                var response = await _httpClient.PutAsJsonAsync($"{_apiUrl}/{id}", usuario);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<Usuario>();
                return result ?? throw new InvalidOperationException("Error al actualizar el usuario");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al actualizar el usuario: {ex.Message}", ex);
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_apiUrl}/{id}");
                response.EnsureSuccessStatusCode();
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
    }
}