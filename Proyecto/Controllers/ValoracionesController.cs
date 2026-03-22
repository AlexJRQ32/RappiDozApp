using Microsoft.AspNetCore.Mvc;
using RappiDozApp.Data;
using RappiDozApp.Models;

namespace RappiDozApp.Controllers
{
    public class ValoracionesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ValoracionesController(ApplicationDbContext context) => _context = context;

        // GET: Valoracion/Crear
        // GET: Valoracion/Crear
        public IActionResult Crear()
        {
            // Cambiamos View por PartialView para que no cargue el _Layout
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

            // Como estamos en un modal, lo ideal es devolver un JSON de éxito
            // para que SweetAlert o JS cierren el modal y recarguen.
            return Json(new { success = true, message = "¡Gracias por tu calificación!" });
        }
    }
}
