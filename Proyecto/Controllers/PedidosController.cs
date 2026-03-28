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

        // FIX: Evita el error 404 cuando se accede a /Pedidos
        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction("Movimientos");
        }

        public async Task<IActionResult> Seguimiento(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.Restaurante)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (pedido == null) return NotFound();

            // Cascada de ubicación: Restaurante > San José Centro (Default)
            decimal latO = 9.9331m;
            decimal lngO = -84.0768m;

            if (pedido.Restaurante != null && pedido.Restaurante.Latitud != 0)
            {
                latO = (decimal)pedido.Restaurante.Latitud;
                lngO = (decimal)pedido.Restaurante.Longitud;
            }

            // Enviamos datos limpios al ViewBag con punto decimal
            ViewBag.RepartidorLat = latO.ToString(CultureInfo.InvariantCulture);
            ViewBag.RepartidorLng = lngO.ToString(CultureInfo.InvariantCulture);
            ViewBag.UsuarioLat = (pedido.EntregaLatitud ?? 9.9350m).ToString(CultureInfo.InvariantCulture);
            ViewBag.UsuarioLng = (pedido.EntregaLongitud ?? -84.0850m).ToString(CultureInfo.InvariantCulture);

            ViewBag.NombreCliente = pedido.Usuario?.NombreCompleto ?? "Cliente";
            ViewBag.PedidoId = pedido.Id;

            return View(pedido);
        }

        [HttpGet]
        public async Task<IActionResult> Factura(int id)
        {
            // IMPORTANTE: Cargamos el pedido con sus hijos (Detalles -> Producto) y (MetodoPago)
            var pedido = await _context.Pedidos
                .Include(p => p.MetodoPago)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .Include(p => p.Usuario) // Opcional, si quieres mostrar el nombre del cliente
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
            {
                TempData["MensajeError"] = "No se encontró la factura solicitada.";
                return RedirectToAction("Movimientos");
            }

            return View("~/Views/Pedidos/factura.cshtml", pedido);
        }

        [HttpGet]
        public async Task<IActionResult> Movimientos()
        {
            int? usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null) return RedirectToAction("Login", "Accesos");

            var historial = await _context.Pedidos
                .Include(p => p.Detalles).ThenInclude(d => d.Producto)
                .Where(p => p.UsuarioId == usuarioId)
                .OrderByDescending(p => p.FechaHora)
                .ToListAsync();

            return View("~/Views/Pedidos/movimientos.cshtml", historial);
        }
        #endregion

        #region Procesos

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarPedido(int UbicacionId, int MetodoPagoId)
        {
            int? usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            var emailUsuario = HttpContext.Session.GetString("EmailUsuario");

            if (usuarioId == null) return RedirectToAction("Login", "Accesos");

            var listaCarritoJson = HttpContext.Session.GetString("CarritoRappiDoz");
            if (string.IsNullOrEmpty(listaCarritoJson)) return RedirectToAction("Index", "Carritos");

            var listaCarrito = JsonSerializer.Deserialize<List<CarritoItem>>(listaCarritoJson) ?? new List<CarritoItem>();

            // Obtener coordenadas de la ubicación seleccionada
            var ubicacion = await _context.UbicacionUsuario.FindAsync(UbicacionId);

            // Cálculos
            decimal subtotal = listaCarrito.Sum(x => x.Precio * x.Cantidad);
            decimal descuentoFinal = 0;
            var descuentoStr = HttpContext.Session.GetString("DescuentoValor");
            if (!string.IsNullOrEmpty(descuentoStr))
            {
                decimal.TryParse(descuentoStr, out decimal valor);
                bool esPorc = HttpContext.Session.GetString("EsPorcentaje") == "true";
                descuentoFinal = esPorc ? (subtotal * (valor / 100)) : valor;
            }

            var nuevoPedido = new Pedido
            {
                UsuarioId = usuarioId.Value,
                MetodoPagoId = MetodoPagoId > 0 ? MetodoPagoId : 1, // Por defecto 1 (Efectivo) si no viene
                FechaHora = DateTime.Now,
                Estado = "Pendiente",
                MontoDescuento = descuentoFinal,
                Total = (subtotal + 2000) - descuentoFinal,
                EntregaLatitud = ubicacion?.Latitud,
                EntregaLongitud = ubicacion?.Longitud,
                Detalles = listaCarrito.Select(item => new DetallePedido
                {
                    ProductoId = item.ProductoId,
                    Cantidad = item.Cantidad,
                    PrecioHistorico = item.Precio
                }).ToList()
            };

            try
            {
                _context.Pedidos.Add(nuevoPedido);
                await _context.SaveChangesAsync();

                // Limpieza de sesión
                HttpContext.Session.Remove("CarritoRappiDoz");
                HttpContext.Session.Remove("CuponAplicado");
                HttpContext.Session.SetString("CarritoCount", "0");

                return RedirectToAction("Factura", new { id = nuevoPedido.Id });
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al procesar pedido: " + ex.Message;
                return RedirectToAction("Index", "Carritos");
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
            return pedido == null ? NotFound() : Json(new { estado = pedido.Estado });
        }
        #endregion
    }
}