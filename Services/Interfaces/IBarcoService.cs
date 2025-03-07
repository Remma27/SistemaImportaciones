using API.Models;

namespace Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
public interface IBarcoService
{
    Task<IEnumerable<Barco>> GetAllAsync();
    Task<Barco?> GetByIdAsync(int id);
    Task<Barco> CreateAsync(Barco barco);
    Task<Barco> UpdateAsync(int id, Barco barco);
    Task DeleteAsync(int id);
}