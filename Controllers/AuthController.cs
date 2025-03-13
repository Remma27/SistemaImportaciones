using Microsoft.AspNetCore.Mvc;
using Sistema_de_Gestion_de_Importaciones.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Sistema_de_Gestion_de_Importaciones.Extensions;
using System.Net;

namespace SistemaDeGestionDeImportaciones.Controllers
{
    [AllowAnonymous]
    public class AuthController : Controller
    {
        private readonly IUsuarioService _usuarioService;
        private readonly ILogger<AuthController> _logger;
        private readonly IAntiforgery _antiforgery;

        public AuthController(
            IUsuarioService usuarioService,
            ILogger<AuthController> logger,
            IAntiforgery antiforgery)
        {
            _usuarioService = usuarioService;
            _logger = logger;
            _antiforgery = antiforgery;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult IniciarSesion(string? returnUrl = null)
        {
            // Limita la longitud de returnUrl para evitar redirecciones anidadas
            if (!string.IsNullOrEmpty(returnUrl) && (returnUrl.Length > 200 || returnUrl.Contains("Error")))
            {
                returnUrl = "/";  // Restablece a la página principal si hay un ciclo
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Registrarse()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            var model = new RegistroViewModel
            {
                Nombre = string.Empty,
                Email = string.Empty,
                Password = string.Empty,
                ConfirmPassword = string.Empty
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Registrarse(RegistroViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var mensajesError = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    string mensajeError = mensajesError.Any()
                        ? mensajesError.First()
                        : "Por favor, revisa los datos ingresados";

                    _logger.LogWarning($"Intento de registro con modelo inválido: {mensajeError}");
                    this.Error(mensajeError);
                    return View(model);
                }

                _logger.LogInformation($"Intentando registrar usuario: {model.Email}");

                var resultado = await _usuarioService.RegistrarUsuarioAsync(model);

                _logger.LogInformation($"Resultado del registro: Success={resultado.Success}, Error={resultado.ErrorMessage ?? "ninguno"}");

                if (!resultado.Success)
                {
                    _logger.LogWarning($"Error en registro: {resultado.ErrorMessage}");

                    string mensajeError = FormatearMensajeError(resultado.ErrorMessage);
                    this.Error(mensajeError);
                    ModelState.AddModelError(string.Empty, mensajeError);
                    return View(model);
                }

                _logger.LogInformation($"Usuario registrado exitosamente: {model.Email}");
                this.Success("Usuario registrado correctamente. Ahora puedes iniciar sesión.");
                return RedirectToAction(nameof(IniciarSesion));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error HTTP durante el registro: {Message}", ex.Message);
                string mensajeError = FormatearErrorHttp(ex);
                this.Error(mensajeError);
                ModelState.AddModelError(string.Empty, mensajeError);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado durante el registro: {Message}", ex.Message);
                this.Error($"Error en el registro: {ObtenerMensajeAmigable(ex.Message)}");
                ModelState.AddModelError(string.Empty, $"Error inesperado: {ex.Message}");
                return View(model);
            }
        }

        private string FormatearMensajeError(string? errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage))
                return "Ocurrió un error durante el registro";

            if (errorMessage.Contains("email ya está registrado", StringComparison.OrdinalIgnoreCase))
                return "Este correo electrónico ya está en uso. Por favor utiliza otro.";

            if (errorMessage.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("401", StringComparison.OrdinalIgnoreCase))
                return "Error de autenticación al conectar con el servicio. Por favor intenta más tarde.";

            if (errorMessage.Contains("contraseña", StringComparison.OrdinalIgnoreCase))
                return "La contraseña no cumple con los requisitos de seguridad.";

            return errorMessage;
        }

        private string FormatearErrorHttp(HttpRequestException ex)
        {
            return ex.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "Error de autenticación. Por favor intenta más tarde.",
                HttpStatusCode.BadRequest => "Los datos enviados no son válidos. Revisa la información ingresada.",
                HttpStatusCode.Conflict => "El correo electrónico ya está registrado. Por favor utiliza otro.",
                HttpStatusCode.InternalServerError => "Error en el servidor. Por favor intenta más tarde.",
                _ => $"Error en la conexión: {ObtenerMensajeAmigable(ex.Message)}"
            };
        }

        private string ObtenerMensajeAmigable(string mensaje)
        {
            if (mensaje.Contains("Response status code does not indicate success:"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(mensaje, @"(\d{3}) \(([^)]+)\)");
                if (match.Success)
                    return $"Error {match.Groups[1].Value}: {match.Groups[2].Value}";
            }

            return mensaje;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> IniciarSesion(LoginViewModel model, string? returnUrl = null)
        {
            // Evita ciclos de redirección con returnUrl
            if (!string.IsNullOrEmpty(returnUrl) && (returnUrl.Length > 200 || returnUrl.Contains("Error")))
            {
                returnUrl = "/";
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var resultado = await _usuarioService.IniciarSesionAsync(model);
                if (!resultado.Success)
                {
                    _logger.LogWarning($"Error de inicio de sesión: {resultado.ErrorMessage}");
                    this.Error("Credenciales inválidas");
                    ModelState.AddModelError(string.Empty, resultado.ErrorMessage ?? "Credenciales inválidas");
                    return View(model);
                }

                var usuario = resultado.Data as Usuario;
                if (usuario == null)
                {
                    _logger.LogWarning("Respuesta de usuario nula o inválida");
                    this.Error("Error de autenticación");
                    ModelState.AddModelError(string.Empty, "Error de autenticación");
                    return View(model);
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, usuario.nombre ?? string.Empty),
                    new Claim(ClaimTypes.Email, usuario.email ?? string.Empty),
                    new Claim("UserId", usuario.id.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation($"Usuario {usuario.email} ha iniciado sesión correctamente");
                this.Success("Inicio de sesión exitoso");

                return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                    ? Redirect(returnUrl)
                    : RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                this.Error("Ha ocurrido un error durante el inicio de sesión");
                _logger.LogError(ex, "Error en el proceso de inicio de sesión");
                ModelState.AddModelError(string.Empty, "Ha ocurrido un error durante el inicio de sesión");
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CerrarSesion()
        {
            try
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email);

                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                Response.Cookies.Delete(".AspNetCore.Cookies");

                _antiforgery.GetAndStoreTokens(HttpContext);

                foreach (var cookie in Request.Cookies.Keys)
                {
                    Response.Cookies.Delete(cookie);
                }

                this.Success("Sesión cerrada correctamente");
                _logger.LogInformation($"Usuario {userEmail} ha cerrado sesión");
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                this.Error("Error al cerrar sesión");
                _logger.LogError(ex, "Error durante el cierre de sesión");
                return RedirectToAction("Index", "Home");
            }
        }
    }
}