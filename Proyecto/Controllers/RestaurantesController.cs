using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RappiDozApp.Data;
using RappiDozApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RappiDozApp.Controllers
{
    public class RestaurantesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RestaurantesController(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Guardar y Eliminar
        [HttpPost]
        public async Task<IActionResult> Guardar(Restaurante restaurante, IFormFile? fotoArchivo, string? LatitudStr, string? LongitudStr)
        {
            ModelState.Remove("Usuario");
            ModelState.Remove("Categoria");
            ModelState.Remove("Productos");
            ModelState.Remove("Latitud");
            ModelState.Remove("Longitud");

            if (!string.IsNullOrEmpty(LatitudStr) && decimal.TryParse(LatitudStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal lat))
                restaurante.Latitud = lat;
            if (!string.IsNullOrEmpty(LongitudStr) && decimal.TryParse(LongitudStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal lng))
                restaurante.Longitud = lng;

            if (restaurante.UsuarioId == 0)
            {
                var sid = HttpContext.Session.GetInt32("UsuarioId");
                if (sid.HasValue) restaurante.UsuarioId = sid.Value;
            }

            try
            {
                if (fotoArchivo != null && fotoArchivo.Length > 0)
                {
                    using (var ms = new MemoryStream())
                    {
                        await fotoArchivo.CopyToAsync(ms);
                        restaurante.LogoBinario = ms.ToArray();
                        restaurante.ContentType = fotoArchivo.ContentType;
                    }
                }
                else if (restaurante.Id != 0)
                {
                    var restauranteExistente = await _context.Restaurantes
                        .AsNoTracking()
                        .FirstOrDefaultAsync(r => r.Id == restaurante.Id);

                    if (restauranteExistente != null)
                    {
                        restaurante.LogoBinario = restauranteExistente.LogoBinario;
                        restaurante.ContentType = restauranteExistente.ContentType;
                    }
                }

                if (restaurante.Id == 0)
                {
                    _context.Add(restaurante);
                }
                else
                {
                    _context.Update(restaurante);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Restaurante guardado correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al guardar: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            var r = await _context.Restaurantes.FindAsync(id);
            if (r == null) return Json(new { success = false, message = "No encontrado." });
            _context.Restaurantes.Remove(r);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Restaurante eliminado correctamente." });
        }

        #endregion

        #region Mapa
        [HttpGet]
        public IActionResult ObtenerMapaRestaurante()
        {
            ViewBag.EsRestaurante = true;
            return PartialView("Mapa", new UbicacionUsuario());
        }
        #endregion

        #region Vista Pública
        public async Task<IActionResult> Menu(int id)
        {
            var data = await _context.Restaurantes
                .AsNoTracking()
                .Where(m => m.Id == id)
                .Select(r => new
                {
                    r.NombreComercial,
                    r.HoraApertura,
                    r.HoraCierre,
                    Productos = r.Productos.Select(p => new
                    {
                        p.Id,
                        p.Nombre,
                        p.Descripcion,
                        p.Precio,
                        p.ContentType,
                        p.CategoriaId,
                        CategoriaNombre = p.Categoria != null ? p.Categoria.Nombre : "General"
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (data == null) return NotFound();

            var ahora = DateTime.Now.TimeOfDay;
            bool estaAbierto = data.HoraApertura <= data.HoraCierre
                ? ahora >= data.HoraApertura && ahora <= data.HoraCierre
                : ahora >= data.HoraApertura || ahora <= data.HoraCierre;

            if (!estaAbierto)
                return RedirectToAction("Index", "Home");

            var productos = data.Productos.Select(p => new Producto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                Precio = p.Precio,
                ContentType = p.ContentType,
                CategoriaId = p.CategoriaId
            }).ToList();

            var categoriaMap = data.Productos
                .GroupBy(p => p.CategoriaId)
                .ToDictionary(g => g.Key, g => g.First().CategoriaNombre);

            ViewBag.RestauranteNombre = data.NombreComercial;
            ViewBag.CategoriaMap = categoriaMap;
            return View("Restaurante", productos);
        }
        #endregion

        #region Logo
        [HttpGet]
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client)]
        public async Task<IActionResult> Logo(int id)
        {
            var data = await _context.Restaurantes
                .AsNoTracking()
                .Where(r => r.Id == id)
                .Select(r => new { r.LogoBinario, r.ContentType })
                .FirstOrDefaultAsync();

            if (data?.LogoBinario == null || data.LogoBinario.Length == 0)
                return NotFound();

            return File(data.LogoBinario, data.ContentType ?? "image/png");
        }
        #endregion
    }
}
