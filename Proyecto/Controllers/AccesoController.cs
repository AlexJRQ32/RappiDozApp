using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RappiDozApp.Data;
using RappiDozApp.Models;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering; // <--- Necesario para SelectList

namespace RappiDozApp.Controllers
{
    public class AccesoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccesoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- LOGIN ---
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

            var correoLimpio = correo.Trim().ToLower();
            var passLimpia = password.Trim();

            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Restaurantes)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == correoLimpio && u.PasswordHash == passLimpia);

            if (usuario != null)
            {
                HttpContext.Session.SetInt32("UsuarioId", usuario.Id);
                HttpContext.Session.SetString("NombreUsuario", usuario.NombreCompleto);
                HttpContext.Session.SetString("RolUsuario", usuario.Rol?.NombreRol ?? "Cliente");
                HttpContext.Session.SetString("EmailUsuario", usuario.Email);

                var primerRestaurante = usuario.Restaurantes.FirstOrDefault();
                if (primerRestaurante != null)
                {
                    HttpContext.Session.SetInt32("IdRestaurante", primerRestaurante.Id);
                }

                if (usuario.FotoBinaria != null)
                {
                    HttpContext.Session.SetString("FotoUsuario", Convert.ToBase64String(usuario.FotoBinaria));
                }

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Correo o contraseña incorrectos.";
            return View("~/Views/Login/login.cshtml");
        }

        // --- OLVIDASTE CONTRASEÑA ---
        public IActionResult OlvidastePassword()
        {
            return View("~/Views/Login/olvidaste-contrasena.cshtml");
        }

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
                ViewBag.Error = "El correo electrónico no esta registrado.";
            }

            return View("~/Views/Login/olvidaste-contrasena.cshtml");
        }

        // --- SELECTOR ---
        public IActionResult Selector(int usuarioId)
        {
            ViewBag.UsuarioId = usuarioId;
            return View("~/Views/Login/selector.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> AsignarRol(int usuarioId, int rolId)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null) return NotFound();

            usuario.RolId = rolId;
            await _context.SaveChangesAsync();

            if (rolId == 2) // Asumiendo que 2 es Restaurante
            {
                return RedirectToAction("RegistroRestaurante", "Acceso", new { id = usuarioId });
            }

            return RedirectToAction("Index", "Home");
        }

        // --- REGISTRO INFO RESTAURANTE (GET) ---
        public async Task<IActionResult> RegistroRestaurante(int id)
        {
            ViewBag.UsuarioId = id;

            // CARGAMOS LAS CATEGORÍAS PARA EL SELECT
            ViewBag.Categorias = new SelectList(await _context.Categorias.ToListAsync(), "Id", "Nombre");

            return View("~/Views/Login/restaurante-info.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarRestaurante(Restaurante restaurante)
        {
            // Removemos validaciones de navegación para que no den error
            ModelState.Remove("Usuario");
            ModelState.Remove("Productos");
            ModelState.Remove("Categoria");

            if (ModelState.IsValid)
            {
                _context.Restaurantes.Add(restaurante);
                await _context.SaveChangesAsync();

                TempData["MensajeExito"] = "¡Negocio registrado con exito!";
                return RedirectToAction("Login", "Acceso");
            }

            // Si hay error, recargamos las categorías antes de volver a la vista
            ViewBag.UsuarioId = restaurante.UsuarioId;
            ViewBag.Categorias = new SelectList(await _context.Categorias.ToListAsync(), "Id", "Nombre", restaurante.CategoriaId);
            ViewBag.Error = "Por favor, verifique los datos ingresados.";

            return View("~/Views/Login/restaurante-info.cshtml", restaurante);
        }

        public async Task<IActionResult> Salir()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        private bool EnviarEmail(string correoDestino, string passwordRecuperada)
        {
            try
            {
                string correoEmisor = "tu-correo@gmail.com";
                string claveAplicacion = "tu-clave-de-aplicacion";

                MailMessage mail = new MailMessage();
                mail.To.Add(correoDestino);
                mail.From = new MailAddress(correoEmisor, "Soporte Rappi'Doz");
                mail.Subject = "Recuperación de Contraseña - Rappi'Doz";
                mail.Body = $"Tu contraseña actual es: {passwordRecuperada}";
                mail.IsBodyHtml = true;

                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                smtp.EnableSsl = true;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(correoEmisor, claveAplicacion);

                smtp.Send(mail);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<IActionResult> Dashboard()
        {
            // 1. Verificación de sesión
            int? userId = HttpContext.Session.GetInt32("UsuarioId");
            string? rol = HttpContext.Session.GetString("RolUsuario");

            if (userId == null) return RedirectToAction("Login", "Acceso");

            // 2. Inicialización de seguridad (Evita el error de pantalla roja)
            ViewBag.ListaUsuarios = new List<Usuario>();
            ViewBag.ListaRestaurantes = new List<Restaurante>();
            ViewBag.RolUsuario = rol;

            // 3. Lógica según el Rol exacto
            if (rol == "Administrador")
            {
                // El Admin ve TODO el sistema
                ViewBag.ListaUsuarios = await _context.Usuarios
                    .Include(u => u.Rol)
                    .OrderByDescending(u => u.Id)
                    .ToListAsync();

                ViewBag.ListaRestaurantes = await _context.Restaurantes
                    .Include(r => r.Usuario)
                    .Include(r => r.Categoria)
                    .ToListAsync();
            }
            else if (rol == "Restaurante") // <-- Cambiado de "Dueño" a "Restaurante"
            {
                // El usuario tipo Restaurante solo ve sus propios locales
                ViewBag.ListaRestaurantes = await _context.Restaurantes
                    .Include(r => r.Categoria)
                    .Where(r => r.UsuarioId == userId)
                    .ToListAsync();
            }

            return View("~/Views/Dashboard/index.cshtml");
        }
    }
}

