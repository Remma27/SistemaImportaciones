using SistemaDeGestionDeImportaciones.Models;

namespace SistemaDeGestionDeImportaciones.Services.Interfaces;

public interface IBarcoService
{
    Task<IEnumerable<Barco>> GetAllAsync();
    Task<Barco> GetByIdAsync(int id);
    Task<Barco> CreateAsync(Barco barco);
    Task<Barco> UpdateAsync(int id, Barco barco);
    Task DeleteAsync(int id);
}