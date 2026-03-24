using Microsoft.AspNetCore.Mvc;
using RappiDozApp.Data;
using RappiDozApp.Models;

namespace RappiDozApp.Controllers
{
    public class ValoracionesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ValoracionesController(ApplicationDbContext context) => _context = context;

        #region Vistas
        public IActionResult Crear()
        {
            return PartialView("~/Views/Valoraciones/calificanos.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar(int estrellas, string comentario, string recomendacion)
        {
            var nuevaValoracion = new Valoracion
            {
                Estrellas = estrellas,
                Comentario = comentario,
                Recomendacion = recomendacion
            };

            _context.Add(nuevaValoracion);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "¡Gracias por tu calificación!" });
        }
        #endregion
    }
}
