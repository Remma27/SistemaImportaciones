using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace Sistema_de_Gestion_de_Importaciones.Controllers
{
    [AllowAnonymous]
    [Route("AuthFix")]
    public class AuthControllerPatches : Controller
    {
        private readonly ILogger<AuthControllerPatches> _logger;

        public AuthControllerPatches(ILogger<AuthControllerPatches> logger)
        {
            _logger = logger;
        }

        [HttpGet("IniciarSesion")]
        public IActionResult IniciarSesion()
        {
            // Create a basic HTML login form for testing
            var html = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8' />
                    <title>Iniciar Sesión</title>
                    <style>
                        body { font-family: Arial, sans-serif; margin: 20px; }
                        .container { max-width: 400px; margin: 0 auto; }
                        .form-group { margin-bottom: 15px; }
                        label { display: block; margin-bottom: 5px; }
                        input[type='text'], input[type='password'] { width: 100%; padding: 8px; }
                        button { padding: 10px 15px; background: #0066cc; color: white; border: none; cursor: pointer; }
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <h1>Iniciar Sesión</h1>
                        <form method='post' action='/AuthFix/DoLogin'>
                            <div class='form-group'>
                                <label for='email'>Correo electrónico:</label>
                                <input type='text' id='email' name='email' required />
                            </div>
                            <div class='form-group'>
                                <label for='password'>Contraseña:</label>
                                <input type='password' id='password' name='password' required />
                            </div>
                            <button type='submit'>Ingresar</button>
                        </form>
                        <p>
                            <a href='/AuthFix/Registrarse'>Crear nueva cuenta</a>
                        </p>
                    </div>
                </body>
                </html>";

            _logger.LogInformation("Rendering simplified login page");
            return Content(html, "text/html");
        }

        [HttpPost("DoLogin")]
        public IActionResult DoLogin(string email, string password)
        {
            _logger.LogInformation("Simplified login attempt for {Email}", email);

            // For testing purposes only, accept any credentials
            return RedirectToAction("Success");
        }

        [HttpGet("Success")]
        public IActionResult Success()
        {
            var html = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8' />
                    <title>Login Successful</title>
                    <style>
                        body { font-family: Arial, sans-serif; margin: 20px; }
                        .container { max-width: 600px; margin: 0 auto; }
                        .success { color: green; }
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <h1 class='success'>¡Inicio de sesión exitoso!</h1>
                        <p>Has iniciado sesión correctamente.</p>
                        <p>
                            <a href='/'>Ir a la página principal</a> |
                            <a href='/AuthFix/IniciarSesion'>Volver al inicio de sesión</a>
                        </p>
                    </div>
                </body>
                </html>";

            return Content(html, "text/html");
        }

        [HttpGet("Registrarse")]
        public IActionResult Registrarse()
        {
            // Create a basic HTML registration form for testing
            var html = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8' />
                    <title>Registrarse</title>
                    <style>
                        body { font-family: Arial, sans-serif; margin: 20px; }
                        .container { max-width: 400px; margin: 0 auto; }
                        .form-group { margin-bottom: 15px; }
                        label { display: block; margin-bottom: 5px; }
                        input[type='text'], input[type='password'], input[type='email'] { width: 100%; padding: 8px; }
                        button { padding: 10px 15px; background: #0066cc; color: white; border: none; cursor: pointer; }
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <h1>Crear nueva cuenta</h1>
                        <form method='post' action='/AuthFix/DoRegistro'>
                            <div class='form-group'>
                                <label for='nombre'>Nombre completo:</label>
                                <input type='text' id='nombre' name='nombre' required />
                            </div>
                            <div class='form-group'>
                                <label for='email'>Correo electrónico:</label>
                                <input type='email' id='email' name='email' required />
                            </div>
                            <div class='form-group'>
                                <label for='password'>Contraseña:</label>
                                <input type='password' id='password' name='password' required />
                            </div>
                            <div class='form-group'>
                                <label for='confirmPassword'>Confirmar contraseña:</label>
                                <input type='password' id='confirmPassword' name='confirmPassword' required />
                            </div>
                            <button type='submit'>Registrarse</button>
                        </form>
                        <p>
                            <a href='/AuthFix/IniciarSesion'>¿Ya tienes una cuenta? Inicia sesión</a>
                        </p>
                    </div>
                </body>
                </html>";

            return Content(html, "text/html");
        }
    }
}
