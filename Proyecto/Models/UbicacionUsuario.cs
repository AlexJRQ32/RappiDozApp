using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RappiDozApp.Models
{
    public class UbicacionUsuario
    {
        [Key]
        public int IdUbicacion { get; set; }
        public string NombreUbicacion { get; set; }
        public decimal Latitud { get; set; }
        public decimal Longitud { get; set; }
        public bool EsPrincipal { get; set; }

        public int IdUsuario { get; set; }

        [ForeignKey("IdUsuario")]
        public virtual Usuario? Usuario { get; set; }
    }
}