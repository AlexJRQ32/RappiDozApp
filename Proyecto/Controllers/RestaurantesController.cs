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

        // ================================================
        // 1. GUARDAR (CREAR Y EDITAR - AJAX)
        // ================================================
        [HttpPost]
        public async Task<IActionResult> Guardar(Restaurante restaurante, IFormFile fotoArchivo)
        {
            // Removemos validaciones de objetos de navegación para evitar conflictos en el ModelState
            ModelState.Remove("Usuarios");
            ModelState.Remove("Categoria");
            ModelState.Remove("Productos");

            try
            {
                // 1. Lógica para procesar la imagen
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
                    // 2. Si es edición y NO se subió foto nueva, mantenemos la anterior
                    // Esto evita que el campo LogoBinario se vuelva nulo al actualizar
                    var restauranteExistente = await _context.Restaurantes
                        .AsNoTracking()
                        .FirstOrDefaultAsync(r => r.Id == restaurante.Id);

                    if (restauranteExistente != null)
                    {
                        restaurante.LogoBinario = restauranteExistente.LogoBinario;
                        restaurante.ContentType = restauranteExistente.ContentType;
                    }
                }

                // 3. Guardar o Actualizar
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

        // ================================================
        // 3. VISTA PÚBLICA DEL MENÚ (CLIENTE)
        // ================================================
        public async Task<IActionResult> Menu(int id)
        {
            var restaurante = await _context.Restaurantes
                .Include(r => r.Productos)
                .ThenInclude(p => p.Categoria)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (restaurante == null) return NotFound();

            ViewBag.RestauranteNombre = restaurante.NombreComercial;
            return View("~/Views/Home/Restaurante.cshtml", restaurante.Productos);
        }
    }
}
