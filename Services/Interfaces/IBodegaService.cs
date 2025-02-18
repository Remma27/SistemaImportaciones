using Sistema_de_Gestion_de_Importaciones.Models;

namespace Sistema_de_Gestion_de_Importaciones.Services.Interfaces
{
    public interface IBodegaService
    {
        Task<IEnumerable<Bodega>> GetAllAsync();
        Task<Bodega> GetByIdAsync(int id);
        Task<Bodega> CreateAsync(Bodega bodega);
        Task<Bodega> UpdateAsync(int id, Bodega bodega);
        Task DeleteAsync(int id);
    }
}