using API.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sistema_de_Gestion_de_Importaciones.Models.ViewModels;
using Sistema_de_Gestion_de_Importaciones.ViewModels;

namespace Sistema_de_Gestion_de_Importaciones.Services.Interfaces
{
    public interface IMovimientoService
    {
        Task<IEnumerable<Movimiento>> GetAllAsync();
        Task<Movimiento> GetByIdAsync(int id);
        Task<Movimiento> CreateAsync(Movimiento movimiento);
        Task<Movimiento> UpdateAsync(int id, Movimiento movimiento);
        Task DeleteAsync(int id);
        Task<List<RegistroRequerimientosViewModel>> GetRegistroRequerimientosAsync(int barcoId);
        Task<RegistroRequerimientosViewModel> GetRegistroRequerimientoByIdAsync(int id);
        Task<IEnumerable<SelectListItem>> GetBarcosSelectListAsync();
        Task<IEnumerable<SelectListItem>> GetImportacionesSelectListAsync();
        Task<IEnumerable<SelectListItem>> GetEmpresasSelectListAsync();
        Task UpdateAsync(int id, RegistroRequerimientosViewModel viewModel, string userId);
        Task<List<InformeGeneralViewModel>> GetInformeGeneralAsync(int barcoId);
        Task<Movimiento> RegistroRequerimientos(int id, Movimiento movimiento);
        Task<Movimiento> InformeGeneral(int id, Movimiento movimiento);
    }
}