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
        private readonly IHttpClientFactory _httpClientFactory;

        public PedidosController(ApplicationDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        #region Vistas
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

            decimal latO = 9.9331m;
            decimal lngO = -84.0768m;

            if (pedido.Restaurante != null && pedido.Restaurante.Latitud != 0)
            {
                latO = (decimal)pedido.Restaurante.Latitud;
                lngO = (decimal)pedido.Restaurante.Longitud;
            }

            ViewBag.RepartidorLat = latO.ToString(CultureInfo.InvariantCulture);
            ViewBag.RepartidorLng = lngO.ToString(CultureInfo.InvariantCulture);
            ViewBag.UsuarioLat = (pedido.EntregaLatitud ?? 9.9350m).ToString(CultureInfo.InvariantCulture);
            ViewBag.UsuarioLng = (pedido.EntregaLongitud ?? -84.0850m).ToString(CultureInfo.InvariantCulture);

            ViewBag.NombreCliente = pedido.Usuario?.NombreCompleto ?? "Cliente";
            ViewBag.PedidoId = pedido.Id;

            return View(pedido);
        }

        [HttpGet]
        public async Task<IActionResult> GetRuta(double latO, double lngO, double latD, double lngD)
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            // Intento 1: OSRM
            try
            {
                var osrmUrl = string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "https://router.project-osrm.org/route/v1/driving/{0},{1};{2},{3}?overview=full&geometries=geojson",
                    lngO, latO, lngD, latD);

                var res = await client.GetAsync(osrmUrl);
                if (res.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
                    var root = doc.RootElement;
                    if (root.GetProperty("code").GetString() == "Ok")
                    {
                        var pts = root.GetProperty("routes")[0]
                            .GetProperty("geometry").GetProperty("coordinates")
                            .EnumerateArray()
                            .Select(c => new { lat = c[1].GetDouble(), lng = c[0].GetDouble() })
                            .ToList();
                        return Json(pts);
                    }
                }
            }
            catch { }

            // Intento 2: Valhalla (server-side, sin CORS)
            try
            {
                var body = new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        locations = new[]
                        {
                            new { lon = lngO, lat = latO },
                            new { lon = lngD, lat = latD }
                        },
                        costing = "auto",
                        directions_options = new { units = "km" }
                    }),
                    System.Text.Encoding.UTF8, "application/json");

                var res = await client.PostAsync("https://valhalla1.openstreetmap.de/route", body);
                if (res.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
                    var root = doc.RootElement;
                    if (root.TryGetProperty("trip", out var trip) &&
                        trip.TryGetProperty("legs", out var legs) &&
                        legs.GetArrayLength() > 0)
                    {
                        var shape = legs[0].GetProperty("shape").GetString()!;
                        return Json(DecodePolyline6(shape));
                    }
                }
            }
            catch { }

            return StatusCode(503);
        }

        private static List<object> DecodePolyline6(string encoded)
        {
            var points = new List<object>();
            int index = 0, len = encoded.Length, lat = 0, lng = 0;
            while (index < len)
            {
                int b, shift = 0, result = 0;
                do { b = encoded[index++] - 63; result |= (b & 0x1f) << shift; shift += 5; } while (b >= 0x20);
                lat += ((result & 1) != 0) ? ~(result >> 1) : (result >> 1);
                shift = 0; result = 0;
                do { b = encoded[index++] - 63; result |= (b & 0x1f) << shift; shift += 5; } while (b >= 0x20);
                lng += ((result & 1) != 0) ? ~(result >> 1) : (result >> 1);
                points.Add(new { lat = lat / 1e6, lng = lng / 1e6 });
            }
            return points;
        }

        [HttpGet]
        public async Task<IActionResult> Factura(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.MetodoPago)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .Include(p => p.Usuario)
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

            var ubicacion = await _context.UbicacionUsuario.FindAsync(UbicacionId);

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
                MetodoPagoId = MetodoPagoId > 0 ? MetodoPagoId : 1,
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