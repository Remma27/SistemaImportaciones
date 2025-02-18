using Sistema_de_Gestion_de_Importaciones.Models;

namespace Sistema_de_Gestion_de_Importaciones.Services.Interfaces
{
    public interface IMovimientoService
    {
        Task<IEnumerable<Movimiento>> GetAllAsync();
        Task<Movimiento> GetByIdAsync(int id);
        Task<Movimiento> CreateAsync(Movimiento movimiento);
        Task<Movimiento> UpdateAsync(int id, Movimiento movimiento);
        Task DeleteAsync(int id);
        Task<Movimiento> InformeGeneral(int importacionId, Movimiento movimiento);
        Task<Movimiento> RegistroRequerimientos(int selectedBarco, Movimiento movimiento);
    }
}