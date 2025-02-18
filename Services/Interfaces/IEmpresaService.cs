using Sistema_de_Gestion_de_Importaciones.Models;

namespace Sistema_de_Gestion_de_Importaciones.Services.Interfaces
{
    public interface IEmpresaService
    {
        Task<IEnumerable<Empresa>> GetAllAsync();
        Task<Empresa> GetByIdAsync(int id);
        Task<Empresa> CreateAsync(Empresa empresa);
        Task<Empresa> UpdateAsync(int id, Empresa empresa);
        Task DeleteAsync(int id);
    }
}