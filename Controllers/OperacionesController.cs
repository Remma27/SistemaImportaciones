using Microsoft.AspNetCore.Mvc;

namespace Sistema_de_Gestion_de_Importaciones.Controllers;

public class OperacionesController : Controller
{
    private readonly ILogger<OperacionesController> _logger;

    public OperacionesController(ILogger<OperacionesController> logger)
    {
        _logger = logger;
    }

    public IActionResult RegistroImportacion()
    {
        return View();
    }

    public IActionResult RegistroPesajes()
    {
        return View();
    }

    public IActionResult EnviarDatos()
    {
        return View();
    }

    public IActionResult InformeGeneral()
    {
        return View();
    }
}