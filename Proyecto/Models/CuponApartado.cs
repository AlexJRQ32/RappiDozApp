using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RappiDozApp.Models
{
    [Table("CuponesApartados")] // Esto asegura que use el nombre exacto de tu script SQL
    public class CuponApartado
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UsuarioEmail { get; set; }

        [Required]
        [StringLength(50)]
        public string Codigo { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Descuento { get; set; }

        public bool EsPorcentaje { get; set; }

        public DateTime FechaReclamado { get; set; } = DateTime.Now;
    }
}
