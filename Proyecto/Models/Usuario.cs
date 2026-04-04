namespace RappiDozApp.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? Telefono { get; set; }

        public virtual ICollection<UbicacionUsuario> Ubicaciones { get; set; } = new List<UbicacionUsuario>();
        public byte[]? FotoBinaria { get; set; }
        public string? ContentType { get; set; }
        public int RolId { get; set; }
        public virtual Rol? Rol { get; set; }
        public bool Activo { get; set; } = true;
        public virtual ICollection<Restaurante> Restaurantes { get; set; } = new List<Restaurante>();
    }
}