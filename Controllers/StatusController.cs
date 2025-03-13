using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Sistema_de_Gestion_de_Importaciones.Controllers
{
    [ApiController]
    [Route("status")]
    [AllowAnonymous]
    public class StatusController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                Status = "Running",
                Timestamp = DateTime.UtcNow,
                Message = "API is operational",
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Not Set"
            });
        }

        [HttpGet("routes")]
        public IActionResult GetRoutes()
        {
            var routes = new[]
            {
                new { Path = "/Auth/IniciarSesion", Description = "Login Page" },
                new { Path = "/Auth/Registrarse", Description = "Registration Page" },
                new { Path = "/Home/Index", Description = "Home Page" },
                new { Path = "/status", Description = "This Status API" }
            };

            return Ok(new { Routes = routes });
        }
    }
}
