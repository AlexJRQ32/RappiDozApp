using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RappiDozApp.Data;
using RappiDozApp.Models;

namespace RappiDozApp.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================================================
        // 1. VISTA PRINCIPAL
        // ================================================
        public async Task<IActionResult> Index()
        {
            var rol = HttpContext.Session.GetString("RolUsuario");
            var userId = HttpContext.Session.GetInt32("UsuarioId");

            if (userId == null) return RedirectToAction("Login", "Accesos");

            ViewBag.TotalRestaurantes = await _context.Restaurantes
                .CountAsync(r => rol == "Administrador" || r.UsuarioId == userId);

            ViewBag.TotalProductos = await _context.Productos
                .CountAsync(p => rol == "Administrador" || p.Restaurante.UsuarioId == userId);

            return View();
        }
        public async Task<IActionResult> Dashboard()
        {
            int? userId = HttpContext.Session.GetInt32("UsuarioId");
            string? rol = HttpContext.Session.GetString("RolUsuario");

            if (userId == null) return RedirectToAction("Login", "Accesos");

            ViewBag.ListaUsuarios = new List<Usuario>();
            ViewBag.ListaRestaurantes = new List<Restaurante>();
            ViewBag.RolUsuario = rol;

            if (rol == "Administrador")
            {
                ViewBag.ListaUsuarios = await _context.Usuarios.Include(u => u.Rol).OrderByDescending(u => u.Id).ToListAsync();
                ViewBag.ListaRestaurantes = await _context.Restaurantes.Include(r => r.Usuario).Include(r => r.Categoria).ToListAsync();
            }
            else if (rol == "Restaurantes")
            {
                ViewBag.ListaRestaurantes = await _context.Restaurantes.Include(r => r.Categoria).Where(r => r.UsuarioId == userId).ToListAsync();
            }

            return View("~/Views/Dashboard/index.cshtml");
        }

        // ================================================
        // 2. CARGA DE LISTADOS (TABLAS)
        // ================================================
        public async Task<IActionResult> GetUsuarios()
        {
            try
            {
                var lista = await _context.Usuarios.Include(u => u.Rol).ToListAsync();
                return PartialView("_UsuarioList", lista);
            }
            catch { return BadRequest("Error al cargar usuarios"); }
        }

        public async Task<IActionResult> GetRestaurantes()
        {
            var rol = HttpContext.Session.GetString("RolUsuario");
            var userId = HttpContext.Session.GetInt32("UsuarioId");

            var restaurantes = await _context.Restaurantes
                .Include(r => r.Categoria)
                .Where(r => rol == "Administrador" || r.UsuarioId == userId)
                .ToListAsync();

            return PartialView("_RestaurantesList", restaurantes);
        }

        public async Task<IActionResult> GetMenus()
        {
            var rol = HttpContext.Session.GetString("RolUsuario");
            var userId = HttpContext.Session.GetInt32("UsuarioId");

            var productos = await _context.Productos
                .Include(p => p.Restaurante)
                .Include(p => p.Categoria)
                .Where(p => rol == "Administrador" || p.Restaurante.UsuarioId == userId)
                .ToListAsync();

            return PartialView("_MenusList", productos);
        }

        public async Task<IActionResult> GetCupones()
        {
            var rol = HttpContext.Session.GetString("RolUsuario");
            if (rol != "Administrador") return Forbid();

            var lista = await _context.Cupones.ToListAsync();
            return PartialView("_CuponesList", lista);
        }

        [HttpGet]
        [Route("Dashboard/GetForm")]
        public async Task<IActionResult> GetForm(string entidad, string accion, int? id)
        {
            // PASO 1: INICIALIZACIÓN PREVENTIVA
            // Esto garantiza que NUNCA sean null, aunque la base de datos falle.
            ViewBag.Categorias = new List<SelectListItem>();
            ViewBag.Restaurantes = new SelectListItem[] { }.ToList();
            ViewBag.Usuarios = new List<SelectListItem>();
            ViewBag.Roles = new List<SelectListItem>();

            try
            {
                var rolSesion = HttpContext.Session.GetString("RolUsuario");
                var usuarioId = HttpContext.Session.GetInt32("UsuarioId") ?? 0;

                // ==========================================
                // ENTIDAD: PRODUCTO (MENÚS)
                // ==========================================
                if (entidad == "Producto")
                {
                    // 1. Cargamos los Restaurantes (Filtrados por dueño o admin)
                    var rests = await _context.Restaurantes
                        .Where(r => r.UsuarioId == usuarioId || rolSesion == "Administrador")
                        .ToListAsync();

                    ViewBag.Restaurantes = rests.Select(r => new SelectListItem
                    {
                        Value = r.Id.ToString(),
                        Text = r.NombreComercial
                    }).ToList();

                    // 2. ¡CLAVE! Cargamos las Categorías (Esto es lo que te faltaba)
                    // Sin esto, el select de categorías en el HTML no tendrá ítems
                    var cats = await _context.Categorias.ToListAsync();
                    ViewBag.Categorias = cats.Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Nombre
                    }).ToList();

                    // 3. Obtenemos el producto o creamos uno nuevo
                    var producto = (id.HasValue && id > 0)
                        ? await _context.Productos.FindAsync(id)
                        : new Producto { Id = 0, Precio = 0 };

                    return PartialView("_FormProducto", producto);
                }

                // ==========================================
                // ENTIDAD: RESTAURANTE (SEDES)
                // ==========================================
                if (entidad == "Restaurantes" || entidad == "Restaurante")
                {
                    // Aquí cargamos las Categorías porque aquí es donde se eligen
                    var cats = await _context.Categorias.ToListAsync();
                    ViewBag.Categorias = cats.Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Nombre
                    }).ToList();

                    if (rolSesion == "Administrador")
                    {
                        var dueños = await _context.Usuarios
                            .Include(u => u.Rol)
                            .Where(u => u.Rol != null && u.Rol.NombreRol == "Restaurantes")
                            .ToListAsync();
                        ViewBag.Usuarios = dueños.Select(d => new SelectListItem
                        {
                            Value = d.Id.ToString(),
                            Text = d.NombreCompleto
                        }).ToList();
                    }

                    var restaurante = (id.HasValue && id > 0)
                        ? await _context.Restaurantes.FindAsync(id)
                        : new Restaurante { UsuarioId = usuarioId };

                    return PartialView("_FormRestaurante", restaurante);
                }

                // ==========================================
                // ENTIDAD: USUARIO
                // ==========================================
                if (entidad == "Usuarios" || entidad == "Usuario")
                {
                    var roles = await _context.Roles.ToListAsync();
                    ViewBag.Roles = roles.Select(r => new SelectListItem
                    {
                        Value = r.Id.ToString(),
                        Text = r.NombreRol
                    }).ToList();

                    var usuario = (id.HasValue && id > 0)
                        ? await _context.Usuarios.FindAsync(id)
                        : new Usuario { Id = 0 };

                    return PartialView("_FormUsuario", usuario);
                }

                // ==========================================
                // ENTIDAD: CUPON
                // ==========================================
                if (entidad == "Cupones" || entidad == "Cupon")
                {
                    // 1. Cargamos las categorías para que aparezcan en el dropdown del cupón
                    var cats = await _context.Categorias.ToListAsync();
                    ViewBag.Categorias = cats.Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Nombre
                    }).ToList();

                    // 2. Obtenemos el cupón o creamos uno nuevo con valores por defecto
                    var cupon = (id.HasValue && id > 0)
                        ? await _context.Cupones.FindAsync(id)
                        : new Cupon
                        {
                            Id = 0,
                            Activo = true,
                            FechaExpiracion = DateTime.Now.AddDays(7),
                            Stock = 10 // Valor inicial sugerido
                        };

                    // 3. Retornamos la vista parcial (Asegúrate de que el nombre del archivo coincida)
                    return PartialView("_FormCupon", cupon);
                }

                return BadRequest("Entidad no soportada");
            }
            catch (Exception ex)
            {
                // El error 500 será capturado por tu SweetAlert en el JS
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        // ================================================
        // 4. MÉTODOS AUXILIARES (JSON)
        // ================================================
        [HttpGet]
        public async Task<IActionResult> GetRolesJson()
        {
            try
            {
                var roles = await _context.Roles
                    .Select(r => new { id = r.Id, nombre = r.NombreRol })
                    .ToListAsync();
                return Json(roles);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}

