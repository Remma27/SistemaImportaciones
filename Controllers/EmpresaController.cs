using Microsoft.AspNetCore.Mvc;
using API.Models;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;

namespace Sistema_de_Gestion_de_Importaciones.Controllers
{
    public class EmpresaController : Controller
    {
        private readonly IEmpresaService _empresaService;
        private readonly ILogger<EmpresaController> _logger;

        public EmpresaController(IEmpresaService empresaService, ILogger<EmpresaController> logger)
        {
            _empresaService = empresaService;
            _logger = logger;
        }

        // GET: Muestra la lista de empresas
        public async Task<IActionResult> Index()
        {
            var empresas = await _empresaService.GetAllAsync();
            return View(empresas);
        }

        // GET: Muestra el formulario para crear una nueva empresa
        public IActionResult Crear()
        {
            return View(new Empresa());
        }

        // POST: Crea una nueva empresa consumiendo el service
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Empresa model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                if (string.IsNullOrWhiteSpace(model.nombreempresa))
                {
                    ModelState.AddModelError("nombreempresa", "El nombre de la empresa es requerido");
                    return View(model);
                }

                await _empresaService.CreateAsync(model);
                _logger.LogInformation($"Empresa '{model.nombreempresa}' creada exitosamente.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al crear la empresa '{model.nombreempresa}'.");
                ModelState.AddModelError("", "Ocurrió un error al crear la empresa.");
                return View(model);
            }
        }

        // GET: Muestra el formulario para editar una empresa
        public async Task<IActionResult> Editar(int id)
        {
            var empresa = await _empresaService.GetByIdAsync(id);
            if (empresa == null)
            {
                return NotFound();
            }
            return View(empresa);
        }

        // POST: Edita una empresa consumiendo el service
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Empresa model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var empresaExistente = await _empresaService.GetByIdAsync(model.id_empresa);
                if (empresaExistente == null)
                {
                    return NotFound();
                }

                await _empresaService.UpdateAsync(model.id_empresa, model);
                _logger.LogInformation($"Empresa '{model.nombreempresa}' actualizada exitosamente.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar la empresa '{model.nombreempresa}'.");
                ModelState.AddModelError("", "Ocurrió un error al actualizar la empresa.");
                return View(model);
            }
        }

        // GET: Muestra el formulario para eliminar una empresa
        public async Task<IActionResult> Eliminar(int id)
        {
            var empresa = await _empresaService.GetByIdAsync(id);
            if (empresa == null)
            {
                return NotFound();
            }
            return View(empresa);
        }
    }
}
