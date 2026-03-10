namespace RappiDozApp.Models
{
    public class CarritoItem
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; }
        public decimal Precio { get; set; }
        public int Cantidad { get; set; }
        public string? ImagenBase64 { get; set; }

        public decimal Total => Precio * Cantidad;
    }
}