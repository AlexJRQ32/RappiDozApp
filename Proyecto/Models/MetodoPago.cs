using System.ComponentModel.DataAnnotations;

namespace RappiDozApp.Models
{
    public class MetodoPago
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Nombre { get; set; }
        public virtual ICollection<Pedido> Pedidos { get; set; }
    }
}
