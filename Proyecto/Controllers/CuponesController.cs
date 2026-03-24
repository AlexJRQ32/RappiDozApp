using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RappiDozApp.Data;
using RappiDozApp.Models;
using Microsoft.AspNetCore.Http;

namespace RappiDozApp.Controllers
{
    public class CuponesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CuponesController(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Vistas
        public async Task<IActionResult> Index()
        {
            var emailUsuario = HttpContext.Session.GetString("EmailUsuario");

            var cuponesVisibles = await _context.Cupones
                .Include(c => c.Categoria)
                .Where(c => c.Activo && c.Stock > 0)
                .ToListAsync();

            if (!string.IsNullOrEmpty(emailUsuario))
            {
                ViewBag.CuponesReclamados = await _context.CuponesApartados
                    .Where(ca => ca.UsuarioEmail == emailUsuario)
                    .Select(ca => ca.Codigo)
                    .ToListAsync();
            }

            return View("~/Views/Cupones/cupones.cshtml", cuponesVisibles);
        }

        [HttpGet]
        public async Task<IActionResult> Detalles(int id)
        {
            var cupon = await _context.Cupones
                .Include(c => c.Categoria)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cupon == null) return NotFound();

            return View("~/Views/Cupones/cupones.cshtml", cupon);
        }

        #endregion

        #region Acciones
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apartar(string codigo)
        {
            var emailUsuario = HttpContext.Session.GetString("EmailUsuario");

            if (string.IsNullOrEmpty(emailUsuario))
            {
                TempData["Error"] = "Debes iniciar sesión para arrancar cupones.";
                return RedirectToAction("Index");
            }

            var cuponMaestro = await _context.Cupones.FirstOrDefaultAsync(c => c.Codigo == codigo);

            if (cuponMaestro == null || cuponMaestro.Stock <= 0)
            {
                TempData["Error"] = "Este cupón ya no está disponible.";
                return RedirectToAction("Index");
            }

            var yaExiste = await _context.CuponesApartados
                .AnyAsync(c => c.Codigo == codigo && c.UsuarioEmail == emailUsuario);

            if (yaExiste)
            {
                TempData["Info"] = "Ya tienes este beneficio.";
                return RedirectToAction("Index");
            }

            cuponMaestro.Stock--;

            var nuevoApartado = new CuponApartado
            {
                Codigo = cuponMaestro.Codigo,
                Descuento = cuponMaestro.Descuento,
                EsPorcentaje = cuponMaestro.EsPorcentaje,
                UsuarioEmail = emailUsuario,
                FechaReclamado = DateTime.Now
            };

            _context.CuponesApartados.Add(nuevoApartado);
            _context.Update(cuponMaestro);
            await _context.SaveChangesAsync();

            TempData["Exito"] = "¡Cupón guardado correctamente!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Guardar(Cupon cupon)
        {
            try
            {
                cupon.Codigo = cupon.Codigo?.ToUpper().Trim();

                if (cupon.CategoriaId == 0) cupon.CategoriaId = null;

                if (cupon.FechaExpiracion == DateTime.MinValue)
                {
                    cupon.FechaExpiracion = DateTime.Now.AddDays(30);
                }

                if (cupon.Id == 0) _context.Add(cupon);
                else _context.Update(cupon);

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cupón guardado con éxito." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            var c = await _context.Cupones.FindAsync(id);
            if (c == null) return Json(new { success = false, message = "Cupón no existe." });
            _context.Cupones.Remove(c);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cupón eliminado." });
        }
        #endregion
    }
}
