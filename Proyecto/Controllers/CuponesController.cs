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

        #region Tienda
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

            return View("Cupones", cuponesVisibles);
        }

        #endregion

        #region Apartar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apartar(string codigo)
        {
            var emailUsuario = HttpContext.Session.GetString("EmailUsuario");
            if (string.IsNullOrEmpty(emailUsuario))
            {
                TempData["Error"] = "Inicia sesión para reclamar cupones.";
                return RedirectToAction("Index");
            }

            var cuponMaestro = await _context.Cupones.FirstOrDefaultAsync(c => c.Codigo == codigo);
            if (cuponMaestro == null || cuponMaestro.Stock <= 0)
            {
                TempData["Error"] = "Cupón agotado.";
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
                Codigo = cuponMaestro.Codigo.Trim().ToUpper(),
                Descuento = cuponMaestro.Descuento,
                EsPorcentaje = cuponMaestro.EsPorcentaje,
                UsuarioEmail = emailUsuario,
                FechaReclamado = DateTime.Now
            };

            _context.CuponesApartados.Add(nuevoApartado);
            _context.Update(cuponMaestro);
            await _context.SaveChangesAsync();

            TempData["Exito"] = "¡Cupón guardado!";
            return RedirectToAction("Index");
        }

        #endregion

        #region Aplicar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AplicarCupon(string codigo)
        {
            var emailUsuario = HttpContext.Session.GetString("EmailUsuario");
            if (string.IsNullOrEmpty(emailUsuario))
            {
                TempData["MensajeError"] = "Sesión expirada.";
                return RedirectToAction("Index", "Carritos");
            }

            if (string.IsNullOrEmpty(codigo)) return RedirectToAction("Index", "Carritos");

            string codLimpio = codigo.Trim().ToUpper();

            var cupón = await _context.CuponesApartados
                .FirstOrDefaultAsync(ca => ca.UsuarioEmail.ToLower() == emailUsuario.ToLower()
                                     && ca.Codigo == codLimpio);

            if (cupón == null)
            {
                TempData["MensajeError"] = "El cupón no es válido o no está en tu billetera.";
                return RedirectToAction("Index", "Carritos");
            }

            HttpContext.Session.SetString("CuponAplicado", cupón.Codigo);
            HttpContext.Session.SetString("DescuentoValor", cupón.Descuento.ToString());
            HttpContext.Session.SetString("EsPorcentaje", cupón.EsPorcentaje.ToString().ToLower());

            TempData["MensajeExito"] = "Cupón " + codLimpio + " aplicado.";
            return RedirectToAction("Index", "Carritos");
        }

        #endregion

        #region Quitar
        public IActionResult QuitarCupon()
        {
            HttpContext.Session.Remove("CuponAplicado");
            HttpContext.Session.Remove("DescuentoValor");
            HttpContext.Session.Remove("EsPorcentaje");

            TempData["MensajeExito"] = "Cupón removido.";
            return RedirectToAction("Index", "Carritos");
        }
        #endregion
    }
}