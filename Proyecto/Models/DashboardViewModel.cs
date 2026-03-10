using System.Collections.Generic;

namespace RappiDozApp.Models
{
    public class DashboardViewModel
    {
        public string Rol { get; set; } = "Cliente";
        public List<Usuario> Usuarios { get; set; } = new List<Usuario>();
        public List<Restaurante> Restaurantes { get; set; } = new List<Restaurante>();
    }
}