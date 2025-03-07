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
                this.Success("Usuario registrado correctamente. Ahora puedes iniciar sesión.");
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
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> IniciarSesion(LoginViewModel model, string? returnUrl = null)
        {
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
                    ModelState.AddModelError(string.Empty, resultado.ErrorMessage ?? "Credenciales inválidas");
                    return View(model);
                }

                var usuario = resultado.Data as Usuario;
                if (usuario == null)
                {
                    _logger.LogWarning("Respuesta de usuario nula o inválida");
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