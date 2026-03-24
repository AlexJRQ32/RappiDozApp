using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RappiDozApp.Data;
using RappiDozApp.Models;

namespace RappiDozApp.Controllers
{
    public class ProductosController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ProductosController(ApplicationDbContext context) => _context = context;

        #region CRUD
        [HttpPost]
        public async Task<IActionResult> Guardar(Producto producto, IFormFile? fotoArchivo)
        {
            ModelState.Remove("Restaurantes");
            ModelState.Remove("Categoria");
            ModelState.Remove("fotoArchivo");

            try
            {
                if (producto.CategoriaId == 0)
                {
                    var catDefecto = await _context.Categorias.FirstOrDefaultAsync();
                    if (catDefecto != null) producto.CategoriaId = catDefecto.Id;
                    else return Json(new { success = false, message = "No existen categorías en la base de datos." });
                }

                if (fotoArchivo != null && fotoArchivo.Length > 0)
                {
                    using var ms = new MemoryStream();
                    await fotoArchivo.CopyToAsync(ms);
                    producto.FotoBinaria = ms.ToArray();
                    producto.ContentType = fotoArchivo.ContentType;
                }
                else if (producto.Id != 0)
                {
                    var original = await _context.Productos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == producto.Id);
                    if (original != null)
                    {
                        producto.FotoBinaria = original.FotoBinaria;
                        producto.ContentType = original.ContentType;
                    }
                }

                if (producto.Id == 0) _context.Add(producto);
                else _context.Update(producto);

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Platillo guardado con éxito." });
            }
            catch (Exception ex)
            {
                var errorReal = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Error: " + errorReal });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            var p = await _context.Productos.FindAsync(id);
            if (p == null) return Json(new { success = false, message = "No encontrado." });
            _context.Productos.Remove(p);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Platillo eliminado." });
        }
        #endregion
    }
}
