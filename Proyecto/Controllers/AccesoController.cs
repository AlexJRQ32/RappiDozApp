using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RappiDozApp.Data;
using RappiDozApp.Models;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RappiDozApp.Controllers
{
    public class AccesoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccesoController(ApplicationDbContext context)
        {
            _context = context;
        }

        #region MÉTODOS DE SESIÓN
        // --- MÉTODO PRIVADO PARA LOGUEAR AUTOMÁTICAMENTE ---
        // Se encarga de llenar todos los campos necesarios en la sesión
        private void EstablecerSesion(Usuario usuario)
        {
            HttpContext.Session.SetInt32("UsuarioId", usuario.Id);
            HttpContext.Session.SetString("NombreUsuario", usuario.NombreCompleto);
            HttpContext.Session.SetString("RolUsuario", usuario.Rol?.NombreRol ?? "Cliente");
            HttpContext.Session.SetString("EmailUsuario", usuario.Email);

            // Intentamos obtener el ID del restaurante si el usuario ya tiene uno asociado
            var primerRestaurante = usuario.Restaurantes?.FirstOrDefault();
            if (primerRestaurante != null)
            {
                HttpContext.Session.SetInt32("IdRestaurante", primerRestaurante.Id);
            }

            if (usuario.FotoBinaria != null)
            {
                HttpContext.Session.SetString("FotoUsuario", Convert.ToBase64String(usuario.FotoBinaria));
            }
        }
        #endregion

        #region LOGIN Y LOGOUT
        public IActionResult Login()
        {
            return View("~/Views/Login/login.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string correo, string password)
        {
            if (string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Por favor, complete todos los campos.";
                return View("~/Views/Login/login.cshtml");
            }

            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Restaurantes)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == correo.Trim().ToLower() && u.PasswordHash == password.Trim());

            if (usuario != null)
            {
                EstablecerSesion(usuario);
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Correo o contraseña incorrectos.";
            return View("~/Views/Login/login.cshtml");
        }

        public async Task<IActionResult> Salir()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
        #endregion

        #region FLUJO DE REGISTRO Y ROLES
        // Muestra la vista para elegir entre Cliente o Restaurante
        public IActionResult Selector(int usuarioId)
        {
            ViewBag.UsuarioId = usuarioId;
            return View("~/Views/Login/selector.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> AsignarRol(int usuarioId, int rolId)
        {
            var usuario = await _context.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == usuarioId);
            if (usuario == null) return NotFound();

            usuario.RolId = rolId;
            await _context.SaveChangesAsync();

            // Si el rol es Restaurante (ID 2), lo mandamos a llenar la info del local
            if (rolId == 2)
            {
                return RedirectToAction("RegistroRestaurante", "Acceso", new { id = usuarioId });
            }

            // SI ES CLIENTE: Lo logueamos automáticamente y enviamos al Home
            // Recargamos el usuario para asegurar que el include del Rol traiga el nombre nuevo
            var usuarioFinal = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id == usuarioId);

            EstablecerSesion(usuarioFinal);
            return RedirectToAction("Index", "Home");
        }

        // GET: Vista para registrar los datos del Restaurante
        public async Task<IActionResult> RegistroRestaurante(int id)
        {
            ViewBag.UsuarioId = id;
            ViewBag.Categorias = new SelectList(await _context.Categorias.ToListAsync(), "Id", "Nombre");
            return View("~/Views/Login/restaurante-info.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarRestaurante(Restaurante restaurante)
        {
            // Limpiamos validaciones de objetos relacionados para evitar que el ModelState sea inválido
            ModelState.Remove("Usuario");
            ModelState.Remove("Productos");
            ModelState.Remove("Categoria");

            if (ModelState.IsValid)
            {
                _context.Restaurantes.Add(restaurante);
                await _context.SaveChangesAsync();

                // LOGUEO AUTOMÁTICO: Una vez creado el restaurante, iniciamos sesión para el dueño
                var usuarioConRol = await _context.Usuarios
                    .Include(u => u.Rol)
                    .Include(u => u.Restaurantes)
                    .FirstOrDefaultAsync(u => u.Id == restaurante.UsuarioId);

                if (usuarioConRol != null)
                {
                    EstablecerSesion(usuarioConRol);
                }

                TempData["MensajeExito"] = "¡Negocio registrado con éxito!";
                return RedirectToAction("Index", "Home");
            }

            // Si falla, recargamos la vista con los errores
            ViewBag.UsuarioId = restaurante.UsuarioId;
            ViewBag.Categorias = new SelectList(await _context.Categorias.ToListAsync(), "Id", "Nombre", restaurante.CategoriaId);
            return View("~/Views/Login/restaurante-info.cshtml", restaurante);
        }
        #endregion

        #region DASHBOARD Y RECUPERACIÓN
        public async Task<IActionResult> Dashboard()
        {
            int? userId = HttpContext.Session.GetInt32("UsuarioId");
            string? rol = HttpContext.Session.GetString("RolUsuario");

            if (userId == null) return RedirectToAction("Login", "Acceso");

            ViewBag.ListaUsuarios = new List<Usuario>();
            ViewBag.ListaRestaurantes = new List<Restaurante>();
            ViewBag.RolUsuario = rol;

            if (rol == "Administrador")
            {
                ViewBag.ListaUsuarios = await _context.Usuarios.Include(u => u.Rol).OrderByDescending(u => u.Id).ToListAsync();
                ViewBag.ListaRestaurantes = await _context.Restaurantes.Include(r => r.Usuario).Include(r => r.Categoria).ToListAsync();
            }
            else if (rol == "Restaurante")
            {
                ViewBag.ListaRestaurantes = await _context.Restaurantes.Include(r => r.Categoria).Where(r => r.UsuarioId == userId).ToListAsync();
            }

            return View("~/Views/Dashboard/index.cshtml");
        }

        public IActionResult OlvidastePassword() => View("~/Views/Login/olvidaste-contrasena.cshtml");

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OlvidastePassword(string correo)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == correo);
            if (usuario != null)
            {
                bool enviado = EnviarEmail(usuario.Email, usuario.PasswordHash);
                ViewBag.Mensaje = enviado ? "Se ha enviado tu contraseña actual." : "Error al enviar correo.";
            }
            else
            {
                ViewBag.Error = "El correo electrónico no está registrado.";
            }
            return View("~/Views/Login/olvidaste-contrasena.cshtml");
        }

        private bool EnviarEmail(string correoDestino, string passwordRecuperada)
        {
            try
            {
                // Configurar con tus credenciales reales
                string correoEmisor = "tu-correo@gmail.com";
                string claveAplicacion = "tu-clave";

                MailMessage mail = new MailMessage();
                mail.To.Add(correoDestino);
                mail.From = new MailAddress(correoEmisor, "Soporte Rappi'Doz");
                mail.Subject = "Recuperación de Contraseña";
                mail.Body = $"Tu contraseña actual es: {passwordRecuperada}";
                mail.IsBodyHtml = true;

                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(correoEmisor, claveAplicacion)
                };

                smtp.Send(mail);
                return true;
            }
            catch { return false; }
        }
        #endregion
    }
}