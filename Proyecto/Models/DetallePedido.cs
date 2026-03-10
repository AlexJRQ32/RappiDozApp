namespace RappiDozApp.Models
{
    public class DetallePedido
    {
        public int Id { get; set; }
        public int PedidoId { get; set; }
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioHistorico { get; set; }

        public virtual Pedido? Pedido { get; set; }
        public virtual Producto? Producto { get; set; }
    }
}