using System.ComponentModel.DataAnnotations.Schema; // Necesario para [NotMapped]

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
        public bool Activo { get; set; } = true;

        public virtual Restaurante? Restaurante { get; set; }
        public virtual Categoria? Categoria { get; set; }

        // PROPIEDAD NUEVA (No se guarda en la DB, solo vive en la memoria)
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
                return "/UI-HTML-CSS/img/default-food.png"; // Imagen por defecto
            }
        }
    }
}