using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RappiDozApp.Data;
using RappiDozApp.Models;
using System.Globalization;

namespace RappiDozApp.Controllers
{
    public class PedidoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PedidoController(ApplicationDbContext context)
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
            if (usuarioSesionId == null) return RedirectToAction("Login", "Acceso");

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

            return View("~/Views/Pedido/seguimiento.cshtml");
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