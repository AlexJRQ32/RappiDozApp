using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using RappiDozApp.Data;
using RappiDozApp.Models;

namespace RappiDozApp.Controllers
{
    public class CarritoController : Controller
    {
        // LLAVES DE SESIÓN UNIFICADAS (Usa estas siempre)
        private const string SESSION_KEY = "CarritoRappiDoz";
        private const string BADGE_KEY = "CarritoCount";

        private readonly ApplicationDbContext _context;

        public CarritoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- VISTA PRINCIPAL DEL CARRITO ---
        public async Task<IActionResult> Index()
        {
            int? userId = HttpContext.Session.GetInt32("UsuarioId");
            if (userId == null) return RedirectToAction("Login", "Acceso");

            var usuario = await _context.Usuarios.FindAsync(userId);

            // CARGAR UBICACIONES: Esto permite que el select en la vista tenga datos
            var ubicaciones = await _context.UbicacionUsuario
                .Where(u => u.IdUsuario == userId.Value)
                .OrderByDescending(u => u.IdUbicacion)
                .ToListAsync();

            ViewBag.Ubicaciones = ubicaciones;

            // 1. Billetera
            ViewBag.CuponesApartados = _context.CuponesApartados
                .Where(c => c.UsuarioEmail == usuario.Email).ToList();

            // 2. Carrito
            var lista = ObtenerCarritoDeSesion();
            decimal subtotal = lista.Sum(x => x.Precio * x.Cantidad);

            // 3. Recuperar cupón
            string codigoCupon = HttpContext.Session.GetString("CuponAplicado");
            decimal descuentoMonetario = 0;

            if (!string.IsNullOrEmpty(codigoCupon))
            {
                decimal.TryParse(HttpContext.Session.GetString("DescuentoValor"), out decimal valor);
                bool esPorc = HttpContext.Session.GetString("EsPorcentaje") == "true";
                descuentoMonetario = esPorc ? (subtotal * (valor / 100)) : valor;
            }

            ViewBag.Subtotal = subtotal;
            ViewBag.Descuento = descuentoMonetario;
            ViewBag.CodigoAplicado = codigoCupon;

            return View("~/Views/Navbar/carrito.cshtml", lista);
        }

        [HttpPost]
        public async Task<IActionResult> EliminarUbicacion(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UsuarioId");
            if (userId == null) return Json(new { success = false, message = "Sesión expirada" });

            var ubicacion = await _context.UbicacionUsuario
                .FirstOrDefaultAsync(u => u.IdUbicacion == id && u.IdUsuario == userId);

            if (ubicacion != null)
            {
                _context.UbicacionUsuario.Remove(ubicacion);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "No se encontró la ubicación" });
        }

        // --- AGREGAR PRODUCTO (Asegúrate de enviar 'imagen' desde la vista) ---
        [HttpPost]
        public IActionResult Agregar(int productoId, string nombre, decimal precio, string imagen)
        {
            // Si llega 0, es que el nombre del input en el HTML sigue mal
            if (productoId == 0) return Json(new { success = false, message = "ID no recibido" });

            var lista = ObtenerCarritoDeSesion();
            var itemExistente = lista.FirstOrDefault(x => x.ProductoId == productoId);

            if (itemExistente != null)
            {
                itemExistente.Cantidad++;
            }
            else
            {
                lista.Add(new CarritoItem
                {
                    ProductoId = productoId,
                    Nombre = nombre,
                    Precio = precio,
                    ImagenBase64 = imagen,
                    Cantidad = 1
                });
            }

            GuardarCarritoEnSesion(lista);

            // DEVOLVEMOS JSON para que el JavaScript de tu vista reciba la confirmación
            return Json(new
            {
                success = true,
                totalItems = lista.Sum(x => x.Cantidad)
            });
        }

        // --- ELIMINAR PRODUCTO ---
        public IActionResult Eliminar(int id)
        {
            var lista = ObtenerCarritoDeSesion();
            lista.RemoveAll(x => x.ProductoId == id);
            GuardarCarritoEnSesion(lista);
            return RedirectToAction("Index");
        }

        // --- APLICAR CUPÓN (Desde la lista de "Mis Cupones") ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AplicarCupon(string codigoCupon)
        {
            var emailSession = HttpContext.Session.GetString("EmailUsuario");
            if (string.IsNullOrEmpty(emailSession)) return RedirectToAction("Login", "Acceso");

            // Buscamos el cupón en la tabla CuponesApartados filtrando por Código y Email
            var cupon = _context.CuponesApartados
                .FirstOrDefault(c => c.Codigo == codigoCupon && c.UsuarioEmail == emailSession);

            if (cupon != null)
            {
                // Guardamos todo en sesión con nombres estandarizados
                HttpContext.Session.SetString("CuponAplicado", cupon.Codigo);
                HttpContext.Session.SetString("DescuentoValor", cupon.Descuento.ToString());
                HttpContext.Session.SetString("EsPorcentaje", cupon.EsPorcentaje.ToString().ToLower());

                TempData["MensajeExito"] = "¡Cupón " + cupon.Codigo + " aplicado!";
            }
            else
            {
                TempData["MensajeError"] = "El cupón no es válido o ya fue utilizado.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarPedido()
        {
            // 1. Validaciones de Usuario y Carrito
            int? usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            var emailUsuario = HttpContext.Session.GetString("EmailUsuario");
            if (usuarioId == null || string.IsNullOrEmpty(emailUsuario)) return RedirectToAction("Login", "Acceso");

            var listaCarrito = ObtenerCarritoDeSesion();
            if (listaCarrito == null || !listaCarrito.Any())
            {
                TempData["MensajeError"] = "El carrito está vacío.";
                return RedirectToAction("Index");
            }

            // 2. Cálculos de Totales y Descuento
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
                LimpiarSesionPostCompra();

                return RedirectToAction("Factura", new { id = nuevoPedido.Id });
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al procesar el pedido: " + ex.Message;
                return RedirectToAction("Index");
            }
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
            return View("~/Views/Navbar/factura.cshtml", pedido);
        }

        // --- ACTUALIZAR CANTIDAD (Para los botones + y -) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ActualizarCantidad(int productoId, string accion)
        {
            var lista = ObtenerCarritoDeSesion();
            var item = lista.FirstOrDefault(x => x.ProductoId == productoId);

            if (item != null)
            {
                if (accion == "aumentar")
                {
                    item.Cantidad++;
                }
                else if (accion == "disminuir")
                {
                    item.Cantidad--;
                    // Si la cantidad llega a 0, eliminamos el producto automáticamente
                    if (item.Cantidad <= 0)
                    {
                        lista.Remove(item);
                    }
                }

                GuardarCarritoEnSesion(lista);
            }

            return RedirectToAction("Index");
        }

        // --- MÉTODOS DE AYUDA (Helpers) ---

        private List<CarritoItem> ObtenerCarritoDeSesion()
        {
            var json = HttpContext.Session.GetString(SESSION_KEY);
            return string.IsNullOrEmpty(json)
                ? new List<CarritoItem>()
                : JsonSerializer.Deserialize<List<CarritoItem>>(json) ?? new List<CarritoItem>();
        }

        private void GuardarCarritoEnSesion(List<CarritoItem> carrito)
        {
            string json = JsonSerializer.Serialize(carrito);
            HttpContext.Session.SetString(SESSION_KEY, json);
            HttpContext.Session.SetString(BADGE_KEY, carrito.Sum(x => x.Cantidad).ToString());
        }

        private void LimpiarSesionPostCompra()
        {
            HttpContext.Session.Remove(SESSION_KEY);
            HttpContext.Session.Remove("CuponActivo");
            HttpContext.Session.Remove("DescuentoValor");
            HttpContext.Session.Remove("EsPorcentaje");
            HttpContext.Session.SetString(BADGE_KEY, "0");
        }


    }
}