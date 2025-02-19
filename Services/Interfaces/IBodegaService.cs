using API.Models;

namespace Sistema_de_Gestion_de_Importaciones.Services.Interfaces
{
    public interface IBodegaService
    {
        Task<IEnumerable<Empresa_Bodegas>> GetAllAsync();
        Task<Empresa_Bodegas> GetByIdAsync(int id);
        Task<Empresa_Bodegas> CreateAsync(Empresa_Bodegas bodega);
        Task<Empresa_Bodegas> UpdateAsync(int id, Empresa_Bodegas bodega);
        Task DeleteAsync(int id);
    }
}