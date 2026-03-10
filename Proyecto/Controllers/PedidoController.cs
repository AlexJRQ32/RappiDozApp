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
            // 1. Verificación de seguridad de sesión
            var usuarioSesionId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioSesionId == null) return RedirectToAction("Login", "Acceso");

            // 2. Cargar datos del pedido y del usuario
            var usuarioLogueado = await _context.Usuarios.FindAsync(usuarioSesionId);
            var pedido = await _context.Pedidos.FindAsync(id);

            if (pedido == null || usuarioLogueado == null) return NotFound();

            // 3. LÓGICA DE SIMULACIÓN (Disparador de la moto)
            // Si el pedido está 'Pendiente' o 'Preparando', lo pasamos a 'En Camino'
            // Esto asegura que el JavaScript reciba la señal de iniciar la ruta.
            if (pedido.Estado == "Pendiente" || pedido.Estado == "Preparando")
            {
                pedido.Estado = "En Camino";
                await _context.SaveChangesAsync();
            }

            // 4. FORMATEO DE COORDENADAS (Crucial para JavaScript)
            // Forzamos el uso de punto decimal (.) para evitar que '9,93' rompa el JS.
            var cultura = CultureInfo.InvariantCulture;

            // Destino: Lo que el usuario marcó en el mapa del Index
            double latDest = (double)(usuarioLogueado.Latitud ?? 9.9333m);
            double lngDest = (double)(usuarioLogueado.Longitud ?? -84.0833m);

            // Origen: Punto fijo (Restaurante/Central en Tibás)
            double latOrig = 9.9600;
            double lngOrig = -84.0800;

            // Enviamos todo al ViewBag como string formateado
            ViewBag.UsuarioLat = latDest.ToString(cultura);
            ViewBag.UsuarioLng = lngDest.ToString(cultura);
            ViewBag.RepartidorLat = latOrig.ToString(cultura);
            ViewBag.RepartidorLng = lngOrig.ToString(cultura);

            ViewBag.PedidoId = pedido.Id;
            ViewBag.EstadoActual = pedido.Estado;
            ViewBag.NombreCliente = usuarioLogueado.NombreCompleto;

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