using System;

namespace Sistema_de_Gestion_de_Importaciones.ViewModels
{
    public class HistorialViewModel
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string Tabla { get; set; } = string.Empty;
        public string TipoOperacion { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
        public string? Descripcion { get; set; }
        public string DatosJSON { get; set; } = string.Empty;
    }
}
