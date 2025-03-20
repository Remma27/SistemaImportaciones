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
using System;
using System.Linq;

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
            if (string.IsNullOrEmpty(returnUrl) ||
                returnUrl.Contains("Error") ||
                returnUrl.Length > 100 ||
                returnUrl.Count(c => c == '%') > 5)
            {
                returnUrl = "/";
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
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
                    var errorMessages = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    string errorMessage = errorMessages.Any()
                        ? errorMessages.First()
                        : "Por favor, completa correctamente todos los campos";

                    this.Error(errorMessage);
                    return View(model);
                }

                var result = await _usuarioService.IniciarSesionAsync(model);

                if (result.Success)
                {
                    _logger.LogInformation("Inicio de sesión exitoso para el usuario {Email}", model.Email);
                    
                    // Extract user data from the result
                    if (result.Data != null && result.Data is Dictionary<string, object> userData)
                    {
                        // Try to get user data
                        userData.TryGetValue("UserId", out var userIdObj);
                        userData.TryGetValue("UserName", out var userNameObj);
                        userData.TryGetValue("UserRole", out var userRoleObj);
                        
                        int userId = userIdObj is int id ? id : 0;
                        string userName = userNameObj?.ToString() ?? "Usuario";
                        string userRole = userRoleObj?.ToString() ?? "Usuario";
                        
                        _logger.LogInformation("Datos de usuario extraídos: ID={UserId}, Nombre={UserName}, Rol={UserRole}", 
                            userId, userName, userRole);
                        
                        // Create claims
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                            new Claim(ClaimTypes.Name, userName),
                            new Claim(ClaimTypes.Email, model.Email),
                            new Claim(ClaimTypes.Role, userRole)
                        };

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = model.RememberMe,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                        };

                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);

                        this.Success($"¡Bienvenido(a) {userName}!");

                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Autenticación exitosa pero sin datos de usuario");
                        this.Error("Credenciales válidas pero no se pudo obtener información del usuario.");
                        ModelState.AddModelError(string.Empty, "Credenciales válidas pero no se pudo obtener información del usuario.");
                        return View(model);
                    }
                }
                else
                {
                    _logger.LogWarning("Inicio de sesión fallido para {Email}: {Message}", model.Email, result.Message);
                    string errorMessage = result.Message ?? "Credenciales inválidas";
                    this.Error(errorMessage);
                    ModelState.AddModelError(string.Empty, errorMessage);
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en inicio de sesión para {Email}", model.Email);
                string errorMessage = "Error de conexión con el servidor. Inténtelo de nuevo más tarde.";
                this.Error(errorMessage);
                ModelState.AddModelError(string.Empty, errorMessage);
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
                string mensajeError = "Error al cerrar la sesión";
                if (ex.Message.Contains("Cookie"))
                    mensajeError = "Error al eliminar cookies de sesión";
                else if (ex.Message.Contains("Authentication"))
                    mensajeError = "Error en el sistema de autenticación";

                this.Error(mensajeError);
                _logger.LogError(ex, "Error durante el cierre de sesión: {Message}", ex.Message);
                return RedirectToAction("Index", "Home");
            }
        }
    }
}