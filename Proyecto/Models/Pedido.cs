namespace RappiDozApp.Models
{
    public class Pedido
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public int? CuponId { get; set; }
        public DateTime FechaHora { get; set; } = DateTime.Now;
        public decimal Total { get; set; }
        public string Estado { get; set; } = "Pendiente";
        public decimal? EntregaLatitud { get; set; }
        public decimal? EntregaLongitud { get; set; }

        public virtual Usuario? Usuario { get; set; }
        public virtual Cupon? Cupon { get; set; }
        public decimal MontoDescuento { get; set; }
        public virtual ICollection<DetallePedido>? Detalles { get; set; }
    }
}