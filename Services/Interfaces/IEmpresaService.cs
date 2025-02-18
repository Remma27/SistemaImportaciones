using SistemaDeGestionDeImportaciones.Models;

namespace SistemaDeGestionDeImportaciones.Services.Interfaces
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