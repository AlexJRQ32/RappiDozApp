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

        [HttpGet]
        public async Task<IActionResult> Mapa()
        {
            int? userId = HttpContext.Session.GetInt32("UsuarioId");
            if (userId == null) return Unauthorized();

            var ultimaUbicacion = await _context.UbicacionUsuario
                .Where(u => u.IdUsuario == userId)
                .OrderByDescending(u => u.IdUbicacion)
                .FirstOrDefaultAsync();

            if (ultimaUbicacion == null)
            {
                ultimaUbicacion = new UbicacionUsuario
                {
                    IdUsuario = userId.Value,
                    Latitud = 9.9333m, // La 'm' es para decimal
                    Longitud = -84.0833m,
                    NombreUbicacion = "Nueva Ubicación"
                };
            }

            return PartialView("~/Views/Ubicaciones/Mapa.cshtml", ultimaUbicacion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarUbicacion(string Latitud, string Longitud, string nombreUbicacion = "Mi Ubicación")
        {
            int? userId = HttpContext.Session.GetInt32("UsuarioId");
            if (userId == null) return Json(new { success = false, message = "Sesión expirada" });

            // Conversión segura de string (punto decimal JS) a decimal C#
            bool latOk = decimal.TryParse(Latitud, System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out decimal lat);
            bool lngOk = decimal.TryParse(Longitud, System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out decimal lng);

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

                    // Guardar en sesión para persistencia rápida si es necesario
                    HttpContext.Session.SetString("Latitud", lat.ToString(CultureInfo.InvariantCulture));
                    HttpContext.Session.SetString("Longitud", lng.ToString(CultureInfo.InvariantCulture));

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
    }
}

