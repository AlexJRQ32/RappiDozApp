namespace RappiDozApp.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public byte[]? FotoBinaria { get; set; } // Aquí se guarda la imagen real
        public string? ContentType { get; set; }  // Aquí guardamos si es .jpg, .png, etc.
        public int RolId { get; set; }
        public virtual Rol? Rol { get; set; }

        // Dentro de la clase Usuario, agrega esta línea:
        public virtual ICollection<Restaurante> Restaurantes { get; set; } = new List<Restaurante>();
    }
}