using SistemaDeGestionDeImportaciones.Models;

namespace SistemaDeGestionDeImportaciones.Services.Interfaces
{
    public interface IUsuarioService
    {
        Task<IEnumerable<Usuario>> GetAllAsync();
        Task<Usuario> GetByIdAsync(int id);
        Task<Usuario> CreateAsync(Usuario usuario);
        Task<Usuario> UpdateAsync(int id, Usuario usuario);
        Task DeleteAsync(int id);
    }
}