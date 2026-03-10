using Microsoft.EntityFrameworkCore;
using RappiDozApp.Models;

namespace RappiDozApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Rol> Roles { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Restaurante> Restaurantes { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Cupon> Cupones { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<DetallePedido> DetallePedidos { get; set; }
        public DbSet<Valoracion> Valoraciones { get; set; }
        public DbSet<CuponApartado> CuponesApartados { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Precisión para Monedas
            modelBuilder.Entity<Producto>().Property(p => p.Precio).HasPrecision(18, 2);
            modelBuilder.Entity<Cupon>().Property(c => c.Descuento).HasPrecision(18, 2);
            modelBuilder.Entity<Pedido>().Property(p => p.Total).HasPrecision(18, 2);
            modelBuilder.Entity<DetallePedido>().Property(d => d.PrecioHistorico).HasPrecision(18, 2);

            // Precisión para Coordenadas del Mapa
            modelBuilder.Entity<Usuario>().Property(u => u.Latitud).HasPrecision(18, 10);
            modelBuilder.Entity<Usuario>().Property(u => u.Longitud).HasPrecision(18, 10);
            modelBuilder.Entity<Pedido>().Property(p => p.EntregaLatitud).HasPrecision(18, 10);
            modelBuilder.Entity<Pedido>().Property(p => p.EntregaLongitud).HasPrecision(18, 10);
        }
    }
}