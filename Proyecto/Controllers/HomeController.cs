using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RappiDozApp.Data;
using RappiDozApp.Models;
using System.Diagnostics;

namespace RappiDozApp.Controllers
{
    public class HomeController : Controller
    {
        private const string ViewName = "~/Views/navbar/busqueda.cshtml";
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Home/Index
        public async Task<IActionResult> Index()
        {
            int? userId = HttpContext.Session.GetInt32("UsuarioId");
            bool tieneUbicacion = false;

            if (userId.HasValue)
            {
                // 1. Buscamos si el usuario tiene AL MENOS UNA ubicaciÃ³n guardada
                // en la nueva tabla de UbicacionUsuario
                tieneUbicacion = await _context.UbicacionUsuario
                    .AnyAsync(u => u.IdUsuario == userId.Value);
            }

            // 2. Pasamos el dato a la View (para bloquear el botÃ³n de "Comprar" si no tiene direcciÃ³n)
            ViewBag.TieneUbicacion = tieneUbicacion;

            // 3. Cargamos los restaurantes
            var restaurantes = await _context.Restaurantes.ToListAsync();

            // Ordenamos: Abiertos primero
            return View(restaurantes.OrderByDescending(r => r.EstaAbierto).ToList());
        }

        // GET: Home/Explorar
        public async Task<IActionResult> Explorar(string buscar)
        {
            // 1. Cargamos la base incluyendo CategorÃ­a y Productos (MenÃº)
            var consulta = _context.Restaurantes
                .Include(r => r.Categoria)
                .Include(r => r.Productos)
                .AsQueryable();

            // 2. Filtramos con lÃ³gica 360Â° (Nombre, CategorÃ­a, MenÃº y UbicaciÃ³n)
            if (!string.IsNullOrEmpty(buscar))
            {
                string b = buscar.ToLower().Trim();

                consulta = consulta.Where(r =>
                    // 1. Coincidencia en nombre del restaurante
                    r.NombreComercial.ToLower().Contains(b) ||

                    // 2. Coincidencia en el nombre de la categorÃ­a
                    (r.Categoria != null && r.Categoria.Nombre.ToLower().Contains(b)) ||

                    // 3. Coincidencia en UBICACIÃ“N / DIRECCIÃ“N (Zona, calle, ciudad)
                    r.Direccion.ToLower().Contains(b) ||

                    // 4. Coincidencia en cualquier producto del menÃº
                    r.Productos.Any(p => p.Nombre.ToLower().Contains(b) || p.Descripcion.ToLower().Contains(b))
                );
            }

            // 3. Ejecutamos y ordenamos por estado y nombre
            var resultados = await consulta.ToListAsync();

            var listaOrdenada = resultados
                .OrderByDescending(r => r.EstaAbierto)
                .ThenBy(r => r.NombreComercial)
                .ToList();

            // 4. Retorno a tu vista personalizada
            return View("~/Views/Home/busqueda.cshtml", listaOrdenada);
        }

        public IActionResult Privacy()
        {
            // Ruta estÃ¡ndar: Views/Home/Privacy.cshtml
            return View("~/Views/Home/Privacy.cshtml");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Ruta estÃ¡ndar: Views/Shared/Error.cshtml
            return View("~/Views/Shared/Error.cshtml", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
