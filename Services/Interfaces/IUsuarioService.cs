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
        // Puedes agregar otros métodos, por ejemplo para IniciarSesion:
        Task<OperationResult> IniciarSesionAsync(LoginViewModel model);
    }

    public class OperationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Usuario? Usuario { get; set; }

        // Métodos de fábrica para mayor conveniencia
        public static OperationResult CreateSuccess(Usuario? usuario = null)
        {
            return new OperationResult
            {
                Success = true,
                Usuario = usuario
            };
        }

        public static OperationResult CreateFailure(string errorMessage)
        {
            return new OperationResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }
}