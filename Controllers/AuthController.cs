using Microsoft.AspNetCore.Mvc;

namespace Sistema_de_Gestion_de_Importaciones.Controllers
{
    public class AuthController : Controller
    {
        public IActionResult IniciarSesion()
        {
            return View();
        }

        public IActionResult Registrarse()
        {
            return View();
        }

        [HttpPost]
        public IActionResult IniciarSesion(string email, string password)
        {
            // Aquí irá la lógica de autenticación
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult Registrarse(string email, string password, string confirmPassword)
        {
            // Aquí irá la lógica de registro
            return RedirectToAction("IniciarSesion");
        }
    }
}
