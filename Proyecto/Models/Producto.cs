using System.ComponentModel.DataAnnotations.Schema;

namespace RappiDozApp.Models
{
    public class Producto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal Precio { get; set; }
        public int RestauranteId { get; set; }
        public int CategoriaId { get; set; }
        public byte[]? FotoBinaria { get; set; }
        public string? ContentType { get; set; }

        public virtual Restaurante? Restaurante { get; set; }
        public virtual Categoria? Categoria { get; set; }

        [NotMapped]
        public string ImagenBase64
        {
            get
            {
                if (FotoBinaria != null && FotoBinaria.Length > 0)
                {
                    string base64 = Convert.ToBase64String(FotoBinaria);
                    return $"data:{ContentType ?? "image/png"};base64,{base64}";
                }
                return "/UI-HTML-CSS/img/default-food.png";
            }
        }
    }
}