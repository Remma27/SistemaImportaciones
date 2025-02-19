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

        // GET: Empresa
        public async Task<IActionResult> Index()
        {
            try
            {
                var empresas = await _empresaService.GetAllAsync();
                return View(empresas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener las empresas");
                ViewBag.Error = "Error al cargar las empresas. Por favor, intente más tarde.";
                return View(new List<Empresa>());
            }
        }

        // GET: Empresa/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Empresa/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Empresa model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _empresaService.CreateAsync(model);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear la empresa");
                ModelState.AddModelError("", "Error al crear la empresa. Por favor, intente más tarde.");
                return View(model);
            }
        }

        // GET: Empresa/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var empresa = await _empresaService.GetByIdAsync(id);
            if (empresa == null)
            {
                return NotFound();
            }
            return View(empresa);
        }

        // POST: Empresa/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Empresa model)
        {
            if (id != model.id_empresa)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _empresaService.UpdateAsync(id, model);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar la empresa");
                ModelState.AddModelError("", "Error al actualizar la empresa. Por favor, intente más tarde.");
                return View(model);
            }
        }

        // GET: Empresa/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var empresa = await _empresaService.GetByIdAsync(id);
            if (empresa == null)
            {
                return NotFound();
            }
            return View(empresa);
        }

        // POST: Empresa/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _empresaService.DeleteAsync(id);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar la empresa");
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
