using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sistema_de_Gestion_de_Importaciones.Controllers
{
    [AllowAnonymous]
    public class TestController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Content("<html><body><h1>Test MVC Controller Works!</h1><p>This confirms MVC routing is working.</p></body></html>", "text/html");
        }

        [HttpGet]
        public IActionResult Api()
        {
            return Json(new { message = "Test MVC Controller JSON response works", success = true });
        }
    }
}
