using System.ComponentModel.DataAnnotations;

namespace RappiDozApp.Models
{
    public class Valoracion
    {
        [Key]
        public int Id { get; set; }
        public int Estrellas { get; set; }
        public string? Comentario { get; set; }
        public string Recomendacion { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public int? RestauranteId { get; set; }
        public int? UsuarioId { get; set; }
    }
}