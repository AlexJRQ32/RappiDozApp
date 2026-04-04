using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RappiDozApp.Models
{
    public class Restaurante
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string NombreComercial { get; set; }

        public string Direccion { get; set; }

        [Required(ErrorMessage = "Debes seleccionar una categoría")]
        public int CategoriaId { get; set; }

        [ForeignKey("CategoriaId")]
        public virtual Categoria? Categoria { get; set; }

        [Required]
        public TimeSpan HoraApertura { get; set; }

        [Required]
        public TimeSpan HoraCierre { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }

        [NotMapped]
        public bool EstaAbierto
        {
            get
            {
                var ahora = DateTime.Now.TimeOfDay;
                if (HoraApertura <= HoraCierre)
                    return ahora >= HoraApertura && ahora <= HoraCierre;
                else
                    return ahora >= HoraApertura || ahora <= HoraCierre;
            }
        }

        [Required]
        public int UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual Usuario? Usuario { get; set; }

        public byte[]? LogoBinario { get; set; }
        public string? ContentType { get; set; }

        public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}