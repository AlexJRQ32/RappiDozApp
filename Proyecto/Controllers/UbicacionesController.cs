using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RappiDozApp.Data;
using RappiDozApp.Models;
using System.Globalization;

namespace RappiDozApp.Controllers
{
    public class UbicacionesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UbicacionesController(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Vistas
        [HttpGet]
        public async Task<IActionResult> Mapa(int? usuarioIdRegistro)
        {
            int? userIdSesion = HttpContext.Session.GetInt32("UsuarioId");

            bool esRegistro = usuarioIdRegistro != null;
            ViewBag.EsRegistro = esRegistro;
            ViewBag.EsRestaurante = esRegistro;

            int targetId = usuarioIdRegistro ?? userIdSesion ?? 0;
            if (targetId == 0) return Unauthorized();

            var ultimaUbicacion = await _context.UbicacionUsuario
                .Where(u => u.IdUsuario == targetId)
                .OrderByDescending(u => u.IdUbicacion)
                .FirstOrDefaultAsync();

            ultimaUbicacion ??= new UbicacionUsuario
            {
                IdUsuario = targetId,
                Latitud = 9.9333m,
                Longitud = -84.0833m,
                NombreUbicacion = esRegistro ? null : "Mi Ubicación"
            };

            return PartialView(ultimaUbicacion);
        }
        #endregion

        #region Acciones
        [HttpPost]
        public async Task<IActionResult> GuardarUbicacion(string Latitud, string Longitud, string nombreUbicacion = "Mi Ubicaci\u00f3n")
        {
            int? userId = HttpContext.Session.GetInt32("UsuarioId");
            if (userId == null) return Json(new { success = false, message = "Sesión expirada" });

            bool latOk = decimal.TryParse(Latitud, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal lat);
            bool lngOk = decimal.TryParse(Longitud, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal lng);

            if (latOk && lngOk)
            {
                try
                {
                    var nuevaUbicacion = new UbicacionUsuario
                    {
                        IdUsuario = userId.Value,
                        NombreUbicacion = nombreUbicacion,
                        Latitud = lat,
                        Longitud = lng
                    };

                    _context.UbicacionUsuario.Add(nuevaUbicacion);
                    await _context.SaveChangesAsync();

                    HttpContext.Session.SetString("Latitud", lat.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    HttpContext.Session.SetString("Longitud", lng.ToString(System.Globalization.CultureInfo.InvariantCulture));

                    return Json(new { success = true, message = $"¡Dirección '{nombreUbicacion}' guardada!" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Error de DB: " + ex.Message });
                }
            }
            return Json(new { success = false, message = "Formato de coordenadas inválido" });
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
        #endregion
    }
}