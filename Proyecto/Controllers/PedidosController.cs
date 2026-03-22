using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RappiDozApp.Data;
using RappiDozApp.Models;
using System.Globalization;
using System.Text.Json;
namespace RappiDozApp.Controllers
{
    public class PedidosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PedidosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // VISTA PRINCIPAL DE SEGUIMIENTO
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Seguimiento(int id)
        {
            var usuarioSesionId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioSesionId == null) return RedirectToAction("Login", "Accesos");

            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null) return NotFound();

            // 3. Obtener la ubicación de entrega (Usando decimal ahora)
            var ubicacionEntrega = await _context.UbicacionUsuario
                .Where(u => u.IdUsuario == usuarioSesionId)
                .OrderByDescending(u => u.IdUbicacion)
                .FirstOrDefaultAsync();

            if (ubicacionEntrega == null)
            {
                // Usamos 'm' para indicar que son decimales
                ubicacionEntrega = new UbicacionUsuario { Latitud = 9.9333m, Longitud = -84.0833m };
            }

            // 4. LÓGICA DE SIMULACIÓN
            if (pedido.Estado == "Pendiente" || pedido.Estado == "Preparando")
            {
                pedido.Estado = "En Camino";
                await _context.SaveChangesAsync();
            }

            // 5. FORMATEO (Cambiamos double por decimal)
            var cultura = CultureInfo.InvariantCulture;

            // Usamos decimal para evitar el InvalidCastException
            decimal latDest = ubicacionEntrega.Latitud;
            decimal lngDest = ubicacionEntrega.Longitud;

            // Origen: Restaurante (También en decimal)
            decimal latOrig = 9.9600m;
            decimal lngOrig = -84.0800m;

            // Al convertir a String con InvariantCulture, JavaScript recibirá "9.9600" correctamente
            ViewBag.UsuarioLat = latDest.ToString(cultura);
            ViewBag.UsuarioLng = lngDest.ToString(cultura);
            ViewBag.RepartidorLat = latOrig.ToString(cultura);
            ViewBag.RepartidorLng = lngOrig.ToString(cultura);

            ViewBag.PedidoId = pedido.Id;
            ViewBag.EstadoActual = pedido.Estado;

            return View("~/Views/Pedidos/seguimiento.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> Factura(int id)
        {
            // Crucial: Usar .Include para que la factura tenga los nombres de los productos
            var pedido = await _context.Pedidos
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return RedirectToAction("Index");

            // Ajusta la ruta a tu vista de factura real
            return View("~/Views/Pedidos/factura.cshtml", pedido);
        }

        [HttpGet]
        public async Task<IActionResult> Movimientos()
        {
            // 1. Validar Sesión: Obtenemos el ID del usuario logueado
            int? usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            if (usuarioId == null)
            {
                // Si no hay sesión, redirigir al Login para evitar errores de acceso
                return RedirectToAction("Login", "Accesos");
            }

            // 2. Consulta con "Eager Loading": Traemos el Pedido y sus hijos
            // .Include(p => p.Detalles) trae la lista de productos comprados
            // .ThenInclude(d => d.Producto) trae los nombres/fotos de esos productos
            var historial = await _context.Pedidos
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .Where(p => p.UsuarioId == usuarioId)
                .OrderByDescending(p => p.FechaHora) // Los más recientes arriba
                .ToListAsync();

            // 3. Retornar la vista específica de movimientos
            return View("~/Views/Usuarios/movimientos.cshtml", historial);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarPedido()
        {
            // 1. Validaciones de Usuario y Carrito
            int? usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            var emailUsuario = HttpContext.Session.GetString("EmailUsuario");
            if (usuarioId == null || string.IsNullOrEmpty(emailUsuario)) return RedirectToAction("Login", "Accesos");

            
            var listaCarritoJson = HttpContext.Session.GetString("CarritoRappiDoz");
            if (string.IsNullOrEmpty(listaCarritoJson))
            {
                TempData["MensajeError"] = "El carrito está vacío.";
                return RedirectToAction("Index");
            }

            // 2. Cálculos de Totales y Descuento
            var listaCarrito = JsonSerializer.Deserialize<List<CarritoItem>>(listaCarritoJson) ?? new List<CarritoItem>();
            decimal subtotal = listaCarrito.Sum(x => x.Precio * x.Cantidad);
            decimal descuentoFinal = 0;

            var descuentoStr = HttpContext.Session.GetString("DescuentoValor");
            var esPorcStr = HttpContext.Session.GetString("EsPorcentaje");
            var codigoAplicado = HttpContext.Session.GetString("CuponAplicado");

            if (!string.IsNullOrEmpty(descuentoStr))
            {
                decimal.TryParse(descuentoStr, out decimal valorDescuento);
                bool esPorc = esPorcStr == "true";
                descuentoFinal = esPorc ? (subtotal * (valorDescuento / 100)) : valorDescuento;
            }

            // 3. Mapeo del Pedido
            var nuevoPedido = new Pedido
            {
                UsuarioId = usuarioId.Value,
                FechaHora = DateTime.Now,
                Estado = "Pendiente",
                MontoDescuento = descuentoFinal,
                Total = (subtotal + 2000) - descuentoFinal,
                Detalles = listaCarrito.Select(item => new DetallePedido
                {
                    ProductoId = item.ProductoId,
                    Cantidad = item.Cantidad,
                    PrecioHistorico = item.Precio
                }).ToList()
            };

            try
            {
                // 4. ELIMINACIÓN DEL CUPÓN DE LA TABLA
                if (!string.IsNullOrEmpty(codigoAplicado))
                {
                    var cuponAEliminar = await _context.CuponesApartados
                        .FirstOrDefaultAsync(c => c.Codigo == codigoAplicado && c.UsuarioEmail == emailUsuario);

                    if (cuponAEliminar != null)
                    {
                        _context.CuponesApartados.Remove(cuponAEliminar);
                    }
                }

                // 5. Guardar Pedido y Eliminar Cupón (Transaccional)
                _context.Pedidos.Add(nuevoPedido);
                await _context.SaveChangesAsync();

                // 6. Limpieza Total
                HttpContext.Session.Remove("CarritoRappiDoz");
                HttpContext.Session.Remove("CuponAplicado");
                HttpContext.Session.Remove("CuponActivo");
                HttpContext.Session.Remove("DescuentoValor");
                HttpContext.Session.Remove("EsPorcentaje");
                HttpContext.Session.SetString("CarritoCount", "0");

                return RedirectToAction("Factura", new { id = nuevoPedido.Id });
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al procesar el pedido: " + ex.Message;
                return RedirectToAction("Index");
            }
        }


        // ============================================================
        // API ENDPOINTS (Llamados desde AJAX)
        // ============================================================

        // 1. Consultar estado actual sin recargar la página
        [HttpGet]
        public async Task<IActionResult> ObtenerEstado(int id)
        {
            var pedido = await _context.Pedidos
                .Select(p => new { p.Id, p.Estado })
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound();

            return Json(new { estado = pedido.Estado });
        }

        // 2. Avanzar manualmente el estado (Útil para pruebas o botones de admin)
        [HttpPost]
        public async Task<IActionResult> AvanzarEstado(int id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null) return NotFound();

            if (pedido.Estado == "En Camino")
                pedido.Estado = "Entregado";
            else
                pedido.Estado = "En Camino";

            await _context.SaveChangesAsync();
            return Json(new { nuevoEstado = pedido.Estado });
        }

        // 3. Finalizar el pedido (Llamado automáticamente por el JS al llegar a la meta)
        [HttpPost]
        public async Task<IActionResult> MarcarEntregado(int id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido != null)
            {
                pedido.Estado = "Entregado";
                await _context.SaveChangesAsync();
                return Ok(new { message = "Entrega confirmada en sistema." });
            }
            return NotFound();
        }
    }
}
