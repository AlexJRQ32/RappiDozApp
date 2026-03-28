using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RappiDozApp.Data;
using RappiDozApp.Models;
using System.Diagnostics;

namespace RappiDozApp.Controllers
{
    public class HomeController : Controller
    {
        private const string ViewName = "~/Views/Home/busqueda.cshtml";
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Vistas
        public async Task<IActionResult> Index()
        {
            int? userId = HttpContext.Session.GetInt32("UsuarioId");
            bool tieneUbicacion = false;

            if (userId.HasValue)
            {
                tieneUbicacion = await _context.UbicacionUsuario
                    .AnyAsync(u => u.IdUsuario == userId.Value);
            }

            ViewBag.TieneUbicacion = tieneUbicacion;

            var restaurantes = await _context.Restaurantes
                    .AsNoTracking()
                    .ToListAsync();

            return View(restaurantes.OrderByDescending(r => r.EstaAbierto).ToList());
        }

        public async Task<IActionResult> Explorar(string buscar)
        {
            var consulta = _context.Restaurantes
                .Include(r => r.Categoria)
                .Include(r => r.Productos)
                .AsQueryable();

            if (!string.IsNullOrEmpty(buscar))
            {
                string b = buscar.ToLower().Trim();

                consulta = consulta.Where(r =>
                    r.NombreComercial.ToLower().Contains(b) ||
                    (r.Categoria != null && r.Categoria.Nombre.ToLower().Contains(b)) ||
                    r.Direccion.ToLower().Contains(b) ||
                    r.Productos.Any(p => p.Nombre.ToLower().Contains(b) || p.Descripcion.ToLower().Contains(b))
                );
            }

            var resultados = await consulta.ToListAsync();

            var listaOrdenada = resultados
                .OrderByDescending(r => r.EstaAbierto)
                .ThenBy(r => r.NombreComercial)
                .ToList();

            return View("~/Views/Home/busqueda.cshtml", listaOrdenada);
        }

        public IActionResult Privacy()
        {
            return View("~/Views/Home/Privacy.cshtml");
        }
        #endregion

        #region Errores
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("~/Views/Shared/Error.cshtml", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        #endregion
    }
}
