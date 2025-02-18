using Microsoft.AspNetCore.Mvc.Rendering;
using Sistema_de_Gestion_de_Importaciones.ViewModels;
using Sistema_de_Gestion_de_Importaciones.Models;

namespace Sistema_de_Gestion_de_Importaciones.Services.Interfaces
{
    public interface IRegistroRequerimientosService
    {
        Task<IEnumerable<SelectListItem>> GetBarcosSelectListAsync();
        Task<IEnumerable<SelectListItem>> GetImportacionesSelectListAsync();
        Task<IEnumerable<SelectListItem>> GetEmpresasSelectListAsync();
        Task<List<RegistroRequerimientosViewModel>> GetRegistroRequerimientosAsync(int barcoId);
        Task<RegistroRequerimientosViewModel> GetRegistroRequerimientoByIdAsync(int id);
        Task CreateAsync(Movimiento movimiento);
        Task UpdateAsync(int id, RegistroRequerimientosViewModel viewModel, string userId);
        Task DeleteAsync(int id);
    }
}