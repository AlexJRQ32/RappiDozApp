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

        #region Vistas
        [HttpGet]
        public async Task<IActionResult> Seguimiento(int id)
        {
            var usuarioSesionId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioSesionId == null) return RedirectToAction("Login", "Accesos");

            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null) return NotFound();

            var ubicacionEntrega = await _context.UbicacionUsuario
                .Where(u => u.IdUsuario == usuarioSesionId)
                .OrderByDescending(u => u.IdUbicacion)
                .FirstOrDefaultAsync();

            if (ubicacionEntrega == null)
            {
                ubicacionEntrega = new UbicacionUsuario { Latitud = 9.9333m, Longitud = -84.0833m };
            }

            if (pedido.Estado == "Pendiente" || pedido.Estado == "Preparando")
            {
                pedido.Estado = "En Camino";
                await _context.SaveChangesAsync();
            }

            var cultura = CultureInfo.InvariantCulture;

            decimal latDest = ubicacionEntrega.Latitud;
            decimal lngDest = ubicacionEntrega.Longitud;

            decimal latOrig = 9.9600m;
            decimal lngOrig = -84.0800m;

            ViewBag.UsuarioLat = latDest.ToString(cultura);
            ViewBag.UsuarioLng = lngDest.ToString(cultura);
            ViewBag.RepartidorLat = latOrig.ToString(cultura);
            ViewBag.RepartidorLng = lngOrig.ToString(cultura);

            ViewBag.PedidoId = pedido.Id;
            ViewBag.EstadoActual = pedido.Estado;

            return View("~/Views/Pedidos/Seguimiento.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> Factura(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return RedirectToAction("Index");

            return View("~/Views/Pedidos/factura.cshtml", pedido);
        }

        [HttpGet]
        public async Task<IActionResult> Movimientos()
        {
            int? usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            if (usuarioId == null)
            {
                return RedirectToAction("Login", "Accesos");
            }

            var historial = await _context.Pedidos
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .Where(p => p.UsuarioId == usuarioId)
                .OrderByDescending(p => p.FechaHora)
                .ToListAsync();

            return View("~/Views/Pedidos/movimientos.cshtml", historial);
        }
        #endregion

        #region Procesos
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarPedido()
        {
            int? usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            var emailUsuario = HttpContext.Session.GetString("EmailUsuario");
            if (usuarioId == null || string.IsNullOrEmpty(emailUsuario)) return RedirectToAction("Login", "Accesos");

            var listaCarritoJson = HttpContext.Session.GetString("CarritoRappiDoz");
            if (string.IsNullOrEmpty(listaCarritoJson))
            {
                TempData["MensajeError"] = "El carrito está vacío.";
                return RedirectToAction("Index");
            }

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
                if (!string.IsNullOrEmpty(codigoAplicado))
                {
                    var cuponAEliminar = await _context.CuponesApartados
                        .FirstOrDefaultAsync(c => c.Codigo == codigoAplicado && c.UsuarioEmail == emailUsuario);

                    if (cuponAEliminar != null)
                    {
                        _context.CuponesApartados.Remove(cuponAEliminar);
                    }
                }

                _context.Pedidos.Add(nuevoPedido);
                await _context.SaveChangesAsync();

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
        #endregion


        #region API
        [HttpGet]
        public async Task<IActionResult> ObtenerEstado(int id)
        {
            var pedido = await _context.Pedidos
                .Select(p => new { p.Id, p.Estado })
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound();

            return Json(new { estado = pedido.Estado });
        }

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
        #endregion
    }
}
