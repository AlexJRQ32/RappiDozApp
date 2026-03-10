using System.ComponentModel.DataAnnotations.Schema;

namespace RappiDozApp.Models
{
    public class Cupon
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal Descuento { get; set; }
        public bool EsPorcentaje { get; set; }
        public DateTime? FechaExpiracion { get; set; }
        public bool Activo { get; set; } = true;

        public int Stock { get; set; } = 100;

        public int? CategoriaId { get; set; }

        [ForeignKey("CategoriaId")]
        public virtual Categoria? Categoria { get; set; }
    }
}