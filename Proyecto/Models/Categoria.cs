using System.ComponentModel.DataAnnotations;

namespace RappiDozApp.Models
{
    public class Categoria
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Nombre { get; set; } // Ejemplo: "Pizza", "Hamburguesas"

        // Relación inversa: Una categoría tiene muchos restaurantes
        public virtual ICollection<Restaurante> Restaurantes { get; set; } = new List<Restaurante>();
    }
}