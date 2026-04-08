using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using RappiDozApp.Data;
using RappiDozApp.Models;

namespace RappiDozApp.Controllers
{
    public class CarritosController : Controller
    {
        private const string SESSION_KEY = "CarritoRappiDoz";
        private const string BADGE_KEY = "CarritoCount";

        private readonly ApplicationDbContext _context;

        public CarritosController(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Vistas
        public async Task<IActionResult> Index()
        {
            int? userId = HttpContext.Session.GetInt32("UsuarioId");
            if (userId == null) return RedirectToAction("Login", "Accesos");

            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null) return RedirectToAction("Login", "Accesos");

            var ubicaciones = await _context.UbicacionUsuario
                .Where(u => u.IdUsuario == userId.Value)
                .OrderByDescending(u => u.IdUbicacion)
                .ToListAsync();

            ViewBag.Ubicaciones = ubicaciones;

            ViewBag.CuponesApartados = await _context.CuponesApartados
                .Where(c => c.UsuarioEmail == usuario.Email)
                .ToListAsync();

            var lista = ObtenerCarritoDeSesion();
            decimal subtotal = lista.Sum(x => x.Precio * x.Cantidad);

            string codigoCupon = HttpContext.Session.GetString("CuponAplicado");
            decimal descuentoMonetario = 0;

            if (!string.IsNullOrEmpty(codigoCupon))
            {
                string valorStr = HttpContext.Session.GetString("DescuentoValor");
                if (decimal.TryParse(valorStr, out decimal valor))
                {
                    bool esPorc = HttpContext.Session.GetString("EsPorcentaje") == "true";
                    descuentoMonetario = esPorc ? (subtotal * (valor / 100)) : valor;
                    ViewBag.CodigoAplicado = codigoCupon;
                }
            }

            ViewBag.Subtotal = subtotal;
            ViewBag.Descuento = descuentoMonetario;

            return View("Carrito", lista);
        }
        #endregion

        #region Acciones
        [HttpPost]
        public IActionResult Agregar(int productoId, string nombre, decimal precio)
        {
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
                    Cantidad = 1
                });
            }

            GuardarCarritoEnSesion(lista);

            return Json(new
            {
                success = true,
                totalItems = lista.Sum(x => x.Cantidad)
            });
        }

        public IActionResult Eliminar(int id)
        {
            var lista = ObtenerCarritoDeSesion();
            lista.RemoveAll(x => x.ProductoId == id);
            GuardarCarritoEnSesion(lista);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AplicarCupon(string codigoCupon)
        {
            var emailSession = HttpContext.Session.GetString("EmailUsuario");
            if (string.IsNullOrEmpty(emailSession)) return RedirectToAction("Login", "Accesos");

            var cupon = await _context.CuponesApartados
                .FirstOrDefaultAsync(c => c.Codigo == codigoCupon && c.UsuarioEmail == emailSession);

            if (cupon != null)
            {
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

        public IActionResult QuitarCupon()
        {
            HttpContext.Session.Remove("CuponAplicado");
            HttpContext.Session.Remove("DescuentoValor");
            HttpContext.Session.Remove("EsPorcentaje");

            TempData["MensajeExito"] = "Cupón removido correctamente.";
            return RedirectToAction("Index");
        }

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
                    if (item.Cantidad <= 0)
                    {
                        lista.Remove(item);
                    }
                }

                GuardarCarritoEnSesion(lista);
            }

            return RedirectToAction("Index");
        }
        #endregion

        #region Helpers
        public List<CarritoItem> ObtenerCarritoDeSesion()
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
        #endregion
    }
}
