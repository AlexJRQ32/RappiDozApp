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
    public class AccesosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccesosController(ApplicationDbContext context)
        {
            _context = context;
        }

        #region MÉTODOS DE SESIÓN
        private void EstablecerSesion(Usuario usuario)
        {
            HttpContext.Session.SetInt32("UsuarioId", usuario.Id);
            HttpContext.Session.SetString("NombreUsuario", usuario.NombreCompleto);
            HttpContext.Session.SetString("RolUsuario", usuario.Rol?.NombreRol ?? "Cliente");
            HttpContext.Session.SetString("EmailUsuario", usuario.Email);

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
            return View("~/Views/Accesos/login.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string correo, string password)
        {
            if (string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Por favor, complete todos los campos.";
                return View("~/Views/Accesos/login.cshtml");
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
            return View("~/Views/Accesos/login.cshtml");
        }

        [HttpGet]
        public IActionResult Registrar()
        {
            ModelState.Clear();
            return View("~/Views/Accesos/register.cshtml", new Usuario());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(Usuario usuario, string confirmarPassword)
        {
            if (usuario.PasswordHash != confirmarPassword)
            {
                ModelState.AddModelError("", "Las contraseñas no coinciden.");
                return View("~/Views/Accesos/register.cshtml", usuario);
            }

            ModelState.Remove("Rol");
            ModelState.Remove("Restaurantes");

            if (ModelState.IsValid)
            {
                try
                {
                    usuario.RolId = 1;
                    _context.Add(usuario);
                    await _context.SaveChangesAsync();

                    HttpContext.Session.SetInt32("UsuarioId", usuario.Id);

                    return RedirectToAction("Selector", "Accesos", new { usuarioId = usuario.Id });
                }
                catch
                {
                    ModelState.AddModelError("", "Error al registrar. Intenta con otro correo.");
                }
            }
            return View("~/Views/Accesos/register.cshtml", usuario);
        }

        public async Task<IActionResult> Salir()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
        #endregion

        #region FLUJO DE REGISTRO Y ROLES
        public IActionResult Selector(int usuarioId)
        {
            ViewBag.UsuarioId = usuarioId;
            return View("~/Views/Accesos/selector.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> AsignarRol(int usuarioId, int rolId)
        {
            var usuario = await _context.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == usuarioId);
            if (usuario == null) return NotFound();

            usuario.RolId = rolId;
            await _context.SaveChangesAsync();

            if (rolId == 2)
            {
                return RedirectToAction("RegistroRestaurante", "Accesos", new { id = usuarioId });
            }

            var usuarioFinal = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id == usuarioId);

            EstablecerSesion(usuarioFinal);
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> RegistroRestaurante(int id)
        {
            ViewBag.UsuarioId = id;
            ViewBag.Categorias = new SelectList(await _context.Categorias.ToListAsync(), "Id", "Nombre");
            return View("~/Views/Accesos/restaurante-info.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarRestaurante(Restaurante restaurante)
        {
            ModelState.Remove("Usuarios");
            ModelState.Remove("Productos");
            ModelState.Remove("Categoria");

            if (ModelState.IsValid)
            {
                _context.Restaurantes.Add(restaurante);
                await _context.SaveChangesAsync();

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

            ViewBag.UsuarioId = restaurante.UsuarioId;
            ViewBag.Categorias = new SelectList(await _context.Categorias.ToListAsync(), "Id", "Nombre", restaurante.CategoriaId);
            return View("~/Views/Accesos/restaurante-info.cshtml", restaurante);
        }
        #endregion

        #region RECUPERACIÓN DE CONTRASEÑA
       

        [HttpGet]
        public IActionResult OlvidastePassword() => View("~/Views/Accesos/olvidaste-contrasena.cshtml");

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
            return View("~/Views/Accesos/olvidaste-contrasena.cshtml");
        }

        private bool EnviarEmail(string correoDestino, string passwordRecuperada)
        {
            try
            {
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
