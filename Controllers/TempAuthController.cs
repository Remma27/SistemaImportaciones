using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sistema_de_Gestion_de_Importaciones.Controllers
{
    [AllowAnonymous]
    public class TempAuthController : Controller
    {
        [HttpGet]
        public IActionResult IniciarSesion()
        {
            return Content("<html><body><h1>Test Login Page</h1><p>This is a test login page without authentication.</p></body></html>", "text/html");
        }

        [HttpGet]
        public IActionResult Registrarse()
        {
            return Content("<html><body><h1>Test Registration Page</h1><p>This is a test registration page without authentication.</p></body></html>", "text/html");
        }
    }
}
