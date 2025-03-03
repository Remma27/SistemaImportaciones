using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public class Unidad
    {
        [Key]
        [Required]
        public int id { get; set; }
        [Required]
        public string? nombre { get; set; }
        [Required]
        public decimal valor { get; set; }
    }
}
