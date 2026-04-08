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
        private void EstablecerSesion(Usuario usuario, int? restauranteId = null)
        {
            HttpContext.Session.SetInt32("UsuarioId", usuario.Id);
            HttpContext.Session.SetString("NombreUsuario", usuario.NombreCompleto);
            HttpContext.Session.SetString("RolUsuario", usuario.Rol?.NombreRol ?? "Cliente");
            HttpContext.Session.SetString("EmailUsuario", usuario.Email);

            if (restauranteId.HasValue && restauranteId.Value > 0)
            {
                HttpContext.Session.SetInt32("IdRestaurante", restauranteId.Value);
            }
        }
        #endregion

        #region LOGIN Y LOGOUT
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string correo, string password)
        {
            if (string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Por favor, complete todos los campos.";
                return View();
            }

            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == correo.Trim().ToLower() && u.PasswordHash == password.Trim());

            if (usuario != null)
            {
                var primerRestauranteId = await _context.Restaurantes
                    .Where(r => r.UsuarioId == usuario.Id)
                    .Select(r => (int?)r.Id)
                    .FirstOrDefaultAsync();

                EstablecerSesion(usuario, primerRestauranteId);
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Correo o contraseña incorrectos.";
            return View();
        }

        [HttpGet]
        public IActionResult Registrar()
        {
            ModelState.Clear();
            return View("Register", new Usuario());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(Usuario usuario, string confirmarPassword)
        {
            if (usuario.PasswordHash != confirmarPassword)
            {
                ModelState.AddModelError("", "Las contraseñas no coinciden.");
                return View("Register", usuario);
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
            return View("Register", usuario);
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
            return View();
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

            await _context.Entry(usuario).Reference(u => u.Rol).LoadAsync();

            EstablecerSesion(usuario);
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> RegistroRestaurante(int id)
        {
            ViewBag.UsuarioId = id;
            ViewBag.Categorias = new SelectList(await _context.Categorias.ToListAsync(), "Id", "Nombre");
            return View("RestauranteInfo");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarRestaurante(Restaurante restaurante, string LatitudStr, string LongitudStr)
        {
            ModelState.Remove("Latitud");
            ModelState.Remove("Longitud");

            if (decimal.TryParse(LatitudStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal lat))
                restaurante.Latitud = lat;

            if (decimal.TryParse(LongitudStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal lng))
                restaurante.Longitud = lng;

            if (restaurante.Latitud == 0 || restaurante.Longitud == 0)
                return Json(new { success = false, message = "Ubicación no válida" });

            if (ModelState.IsValid)
            {
                _context.Restaurantes.Add(restaurante);
                await _context.SaveChangesAsync();
                return Json(new { success = true, redirectUrl = Url.Action("Login", "Accesos") });
            }

            var error = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return Json(new { success = false, message = "Datos inválidos: " + error });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerMapaUniversal(int usuarioId)
        {
            var rolId = await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.Id == usuarioId)
                .Select(u => (int?)u.RolId)
                .FirstOrDefaultAsync();

            ViewBag.EsRestaurante = (rolId == 2);

            return PartialView("Mapa", new UbicacionUsuario());
        }
        #endregion

        #region RECUPERACIÓN DE CONTRASEÑA
        [HttpGet]
        public IActionResult OlvidastePassword() => View("OlvidasteContrasena");

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OlvidastePassword(string correo)
        {
            var usuario = await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.Email == correo)
                .Select(u => new { u.Email, u.PasswordHash })
                .FirstOrDefaultAsync();

            if (usuario != null)
            {
                bool enviado = EnviarEmail(usuario.Email, usuario.PasswordHash);
                ViewBag.Mensaje = enviado ? "Se ha enviado tu contraseña actual." : "Error al enviar correo.";
            }
            else
            {
                ViewBag.Error = "El correo electrónico no está registrado.";
            }
            return View("OlvidasteContrasena");
        }

        private bool EnviarEmail(string correoDestino, string passwordRecuperada)
        {
            try
            {
                string correoEmisor = "tu-correo@gmail.com";
                string claveAplicacion = "tu-clave";

                using var mail = new MailMessage();
                mail.To.Add(correoDestino);
                mail.From = new MailAddress(correoEmisor, "Soporte Rappi'Doz");
                mail.Subject = "Recuperación de Contraseña";
                mail.Body = $"Tu contraseña actual es: {passwordRecuperada}";
                mail.IsBodyHtml = true;

                using var smtp = new SmtpClient("smtp.gmail.com", 587)
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
