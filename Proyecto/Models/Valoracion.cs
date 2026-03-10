using System.ComponentModel.DataAnnotations;

namespace RappiDozApp.Models
{
    public class Valoracion
    {
        [Key]
        public int Id { get; set; }
        public int Estrellas { get; set; }
        public string? Comentario { get; set; }
        public string Recomendacion { get; set; } // si, talvez, no
        public DateTime Fecha { get; set; } = DateTime.Now;

        // Relación opcional con el restaurante
        public int? RestauranteId { get; set; }
    }
}