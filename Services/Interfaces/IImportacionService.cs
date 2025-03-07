using API.Models;

namespace Sistema_de_Gestion_de_Importaciones.Services.Interfaces;

public interface IImportacionService
{
    Task<IEnumerable<Importacion>> GetAllAsync();
    Task<Importacion> GetByIdAsync(int id);
    Task<Importacion> CreateAsync(Importacion importacion);
    Task<Importacion> UpdateAsync(int id, Importacion importacion);
    Task DeleteAsync(int id);
}
