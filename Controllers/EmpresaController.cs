using Microsoft.AspNetCore.Mvc;
using API.Models;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using Sistema_de_Gestion_de_Importaciones.Helpers;
using Microsoft.AspNetCore.Authorization;
using Sistema_de_Gestion_de_Importaciones.Extensions;

namespace Sistema_de_Gestion_de_Importaciones.Controllers
{
    [Authorize]
    public class EmpresaController : Controller
    {
        private readonly IEmpresaService _empresaService;
        private readonly ILogger<EmpresaController> _logger;

        public EmpresaController(IEmpresaService empresaService, ILogger<EmpresaController> logger)
        {
            _empresaService = empresaService;
            _logger = logger;
        }

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
                this.Error("Error al cargar las empresas. Por favor, intente más tarde.");
                return View(new List<Empresa>());
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Empresa empresa)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    empresa.idusuario = User.GetUserId();
                    await _empresaService.CreateAsync(empresa);
                    this.Success("Empresa creada correctamente.");
                    return RedirectToAction("Index", "Empresa");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al crear la empresa");
                    ModelState.AddModelError("", "Ocurrió un error al crear la empresa.");
                }
            }
            return View(empresa);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var empresa = await _empresaService.GetByIdAsync(id);
                if (empresa == null)
                {
                    return NotFound();
                }
                return View(empresa);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la empresa para editar");
                this.Error("Error al cargar la empresa.");
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id_empresa,nombreempresa,estatus")] Empresa empresa)
        {
            if (id != empresa.id_empresa)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    empresa.idusuario = User.GetUserId();
                    await _empresaService.UpdateAsync(id, empresa);
                    this.Success("Empresa actualizada correctamente.");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al actualizar la empresa");
                    ModelState.AddModelError("", "Ocurrió un error al actualizar la empresa.");
                }
            }
            return View(empresa);
        }

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

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed([Bind(Prefix = "id_empresa")] int id)
        {
            try
            {
                await _empresaService.DeleteAsync(id);
                this.Success("Empresa eliminada correctamente.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar la empresa con ID {Id}", id);
                var empresa = await _empresaService.GetByIdAsync(id);
                this.Error("Ocurrió un error al eliminar la empresa: " + ex.Message);
                return View("Delete", empresa);
            }
        }
    }
}
