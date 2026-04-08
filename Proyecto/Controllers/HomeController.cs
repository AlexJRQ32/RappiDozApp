using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RappiDozApp.Data;
using RappiDozApp.Models;
using System.Diagnostics;

namespace RappiDozApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Vistas
        public async Task<IActionResult> Index()
        {
            var restaurantes = await _context.Restaurantes
                .AsNoTracking()
                .Select(r => new Restaurante
                {
                    Id = r.Id,
                    NombreComercial = r.NombreComercial,
                    Direccion = r.Direccion,
                    HoraApertura = r.HoraApertura,
                    HoraCierre = r.HoraCierre,
                    ContentType = r.ContentType,
                    CategoriaId = r.CategoriaId,
                    UsuarioId = r.UsuarioId
                })
                .ToListAsync();

            return View(restaurantes.OrderByDescending(r => r.EstaAbierto).ToList());
        }

        public async Task<IActionResult> Explorar(string buscar)
        {
            var consulta = _context.Restaurantes.AsNoTracking().AsQueryable();

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

            var resultados = await consulta
                .Select(r => new Restaurante
                {
                    Id = r.Id,
                    NombreComercial = r.NombreComercial,
                    Direccion = r.Direccion,
                    HoraApertura = r.HoraApertura,
                    HoraCierre = r.HoraCierre,
                    ContentType = r.ContentType,
                    CategoriaId = r.CategoriaId,
                    UsuarioId = r.UsuarioId
                })
                .ToListAsync();

            var listaOrdenada = resultados
                .OrderByDescending(r => r.EstaAbierto)
                .ThenBy(r => r.NombreComercial)
                .ToList();

            return View("Busqueda", listaOrdenada);
        }

        public IActionResult ManualUsuario()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
        #endregion

        #region Errores
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        #endregion
    }
}
