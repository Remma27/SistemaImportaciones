using Sistema_de_Gestion_de_Importaciones.ViewModels;

namespace Sistema_de_Gestion_de_Importaciones.Services.Interfaces
{
    public interface IHistorialService
    {
        Task<HistorialResponseViewModel> ObtenerTodoAsync();
        Task<HistorialViewModel> ObtenerPorIdAsync(int id);
        Task<List<string>> ObtenerTablasDisponiblesAsync();
    }
}
