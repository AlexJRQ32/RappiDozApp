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

            if (userId != null)
            {
                // Buscamos al usuario en la base de datos
                var usuario = await _context.Usuarios.FindAsync(userId);
                // Si movió el marcador (no es 0 ni el centro por defecto), tiene ubicación
                if (usuario != null && usuario.Latitud != 0 && usuario.Latitud != 9.9281m)
                {
                    tieneUbicacion = true;
                }
            }

            ViewBag.TieneUbicacion = tieneUbicacion; // Esto lo lee el HTML para bloquear botones
            var restaurantes = await _context.Restaurantes.Include(r => r.Categoria).ToListAsync();
            return View(restaurantes.OrderByDescending(r => r.EstaAbierto).ToList());
        }

        // GET: Home/Explorar
        public async Task<IActionResult> Explorar(string buscar)
        {
            // 1. Cargamos la base incluyendo Categoría y Productos (Menú)
            var consulta = _context.Restaurantes
                .Include(r => r.Categoria)
                .Include(r => r.Productos)
                .AsQueryable();

            // 2. Filtramos con lógica 360° (Nombre, Categoría, Menú y Ubicación)
            if (!string.IsNullOrEmpty(buscar))
            {
                string b = buscar.ToLower().Trim();

                consulta = consulta.Where(r =>
                    // 1. Coincidencia en nombre del restaurante
                    r.NombreComercial.ToLower().Contains(b) ||

                    // 2. Coincidencia en el nombre de la categoría
                    (r.Categoria != null && r.Categoria.Nombre.ToLower().Contains(b)) ||

                    // 3. Coincidencia en UBICACIÓN / DIRECCIÓN (Zona, calle, ciudad)
                    r.Direccion.ToLower().Contains(b) ||

                    // 4. Coincidencia en cualquier producto del menú
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
            return View("~/Views/Navbar/busqueda.cshtml", listaOrdenada);
        }

        public IActionResult Privacy()
        {
            // Ruta estándar: Views/Home/Privacy.cshtml
            return View("~/Views/Home/Privacy.cshtml");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Ruta estándar: Views/Shared/Error.cshtml
            return View("~/Views/Shared/Error.cshtml", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}