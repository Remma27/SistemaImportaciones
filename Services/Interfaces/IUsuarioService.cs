using Sistema_de_Gestion_de_Importaciones.ViewModels;
using API.Models;

namespace Sistema_de_Gestion_de_Importaciones.Services.Interfaces
{
    public interface IUsuarioService
    {
        Task<IEnumerable<Usuario>> GetAllAsync();
        Task<Usuario> GetByIdAsync(int id);
        Task<Usuario> CreateAsync(Usuario usuario);
        Task<Usuario> UpdateAsync(int id, Usuario usuario);
        Task DeleteAsync(int id);
        Task<OperationResult> RegistrarUsuarioAsync(RegistroViewModel model);
        Task<OperationResult> IniciarSesionAsync(LoginViewModel model);
    }
}