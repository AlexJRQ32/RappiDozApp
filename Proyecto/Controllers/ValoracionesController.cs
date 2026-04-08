using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RappiDozApp.Data;
using RappiDozApp.Models;

namespace RappiDozApp.Controllers
{
    public class ValoracionesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ValoracionesController(ApplicationDbContext context) => _context = context;

        #region Vistas
        public IActionResult Crear(int pedidoId = 0)
        {
            ViewBag.PedidoId = pedidoId;
            return PartialView("Calificanos");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar(int estrellas, string comentario, string recomendacion, int pedidoId = 0)
        {
            int? restauranteId = null;
            if (pedidoId > 0)
            {
                restauranteId = await _context.Pedidos
                    .AsNoTracking()
                    .Where(p => p.Id == pedidoId)
                    .Select(p => p.RestauranteId)
                    .FirstOrDefaultAsync();
            }

            int? usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            var nuevaValoracion = new Valoracion
            {
                Estrellas = estrellas,
                Comentario = comentario,
                Recomendacion = recomendacion,
                RestauranteId = restauranteId,
                UsuarioId = usuarioId
            };

            try
            {
                _context.Add(nuevaValoracion);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "¡Gracias por tu calificación!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "No se pudo guardar la calificación. Intenta de nuevo.", detalle = ex.Message });
            }
        }
        #endregion
    }
}
