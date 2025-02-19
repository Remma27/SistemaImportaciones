using Microsoft.AspNetCore.Mvc;
using Sistema_de_Gestion_de_Importaciones.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using API.Models;
using Microsoft.AspNetCore.Authorization;

namespace SistemaDeGestionDeImportaciones.Controllers
{
    [AllowAnonymous]
    public class AuthController : Controller
    {
        private readonly IUsuarioService _usuarioService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IUsuarioService usuarioService, ILogger<AuthController> logger)
        {
            _usuarioService = usuarioService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult IniciarSesion()
        {
            return View(); // Asegúrate de tener Views/Auth/IniciarSesion.cshtml
        }

        [HttpGet]
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
        public async Task<IActionResult> Registrarse(RegistroViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Intento de registro con modelo inválido");
                    return View(model);
                }

                var resultado = await _usuarioService.RegistrarUsuarioAsync(model);
                if (!resultado.Success)
                {
                    _logger.LogWarning($"Error en registro: {resultado.ErrorMessage}");
                    ModelState.AddModelError(string.Empty, resultado.ErrorMessage ?? "Ocurrió un error durante el registro");
                    return View(model);
                }

                _logger.LogInformation($"Usuario registrado exitosamente: {model.Email}");
                return RedirectToAction(nameof(IniciarSesion));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado durante el registro");
                ModelState.AddModelError(string.Empty, "Ocurrió un error durante el registro");
                return View(model);
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken] // Si deseas desactivar la validación antiforgery en inicio de sesión
        public async Task<IActionResult> IniciarSesion(LoginViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Intento de inicio de sesión con modelo inválido");
                    return View(model);
                }

                var resultado = await _usuarioService.IniciarSesionAsync(model);
                if (!resultado.Success || resultado.Data is null)
                {
                    _logger.LogWarning($"Error en inicio de sesión para {model.Email}");
                    ModelState.AddModelError(string.Empty, resultado.ErrorMessage ?? "Error de autenticación");
                    return View(model);
                }

                var usuario = resultado.Data as Usuario;
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, usuario?.nombre ?? string.Empty),
                    new Claim(ClaimTypes.Email, usuario?.email ?? string.Empty),
                    new Claim("UserId", usuario?.id.ToString() ?? "0")
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

                _logger.LogInformation($"Usuario {model.Email} ha iniciado sesión exitosamente");
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado durante el inicio de sesión");
                ModelState.AddModelError(string.Empty, "Ocurrió un error durante el inicio de sesión");
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
                _logger.LogInformation($"Usuario {userEmail} ha cerrado sesión");
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el cierre de sesión");
                return RedirectToAction("Index", "Home");
            }
        }
    }
}