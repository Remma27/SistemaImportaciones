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

        [Authorize(Roles = "Administrador,Operador")]
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
        [Authorize(Roles = "Administrador,Operador")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Operador")]
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
        [Authorize(Roles = "Administrador,Operador")]
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
        [Authorize(Roles = "Administrador,Operador")]
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
        [Authorize(Roles = "Administrador")]
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
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed([Bind(Prefix = "id_empresa")] int id)
        {
            try
            {
                await _empresaService.DeleteAsync(id);
                this.Success("Empresa eliminada correctamente.");
                return RedirectToAction(nameof(Index));
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("400") && 
                (ex.Message.Contains("importaciones") || ex.Message.Contains("movimientos") || 
                 ex.Message.Contains("asociada") || ex.Message.Contains("relacionada")))
            {
                _logger.LogWarning(ex, $"Intento de eliminar empresa con ID: {id} que tiene relaciones");
                this.Warning("No se puede eliminar esta empresa porque tiene importaciones o movimientos asociados.");
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("importaciones") || 
                ex.Message.Contains("movimientos") || ex.Message.Contains("asociada") || 
                ex.Message.Contains("relacionada"))
            {
                _logger.LogWarning(ex, $"Intento de eliminar empresa con ID: {id} con relaciones");
                this.Warning(ex.Message);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar la empresa con ID {Id}", id);
                
                try {
                    var empresa = await _empresaService.GetByIdAsync(id);
                    this.Error($"Error al eliminar la empresa: {ex.Message}");
                    return View("Delete", empresa);
                }
                catch {
                    this.Error($"Error al eliminar la empresa: {ex.Message}");
                    return RedirectToAction(nameof(Index));
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Operador")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                // Get current empresa
                var empresa = await _empresaService.GetByIdAsync(id);
                if (empresa == null)
                {
                    return Json(new { success = false, message = "Empresa no encontrada" });
                }

                // Toggle status (1 -> 0, 0 -> 1)
                empresa.estatus = empresa.estatus == 1 ? 0 : 1;
                empresa.idusuario = User.GetUserId();

                // Update database
                await _empresaService.UpdateAsync(id, empresa);

                string statusText = empresa.estatus == 1 ? "activada" : "desactivada";
                return Json(new { success = true, message = $"Empresa {statusText} correctamente", newStatus = empresa.estatus });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado de la empresa {Id}", id);
                return Json(new { success = false, message = "Error al cambiar el estado de la empresa" });
            }
        }
    }
}
