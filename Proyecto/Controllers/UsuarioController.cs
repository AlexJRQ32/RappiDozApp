using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RappiDozApp.Data;
using RappiDozApp.Models;
using System.Globalization;

namespace RappiDozApp.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsuarioController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Perfil()
        {
            // 1. Validar sesión
            int? userId = HttpContext.Session.GetInt32("UsuarioId");
            if (userId == null) return Unauthorized(); // El JS manejará esto si la sesión expiró

            // 2. Buscar usuario
            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null) return NotFound();

            // 3. RETORNAR PARTIAL VIEW (Sin Layout)
            // Esto inyecta solo el formulario dentro del modal
            return PartialView("~/Views/CRUDs/users-form.cshtml", usuario);
        }

        [HttpGet]
        public async Task<IActionResult> Mapa()
        {
            int? userId = HttpContext.Session.GetInt32("UsuarioId");
            if (userId == null) return Unauthorized();

            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null) return NotFound();

            // Importante: La ruta debe coincidir exactamente con donde tienes el archivo .cshtml
            return PartialView("~/Views/Navbar/Mapa.cshtml", usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarPerfil(Usuario model, IFormFile? fotoArchivo, string? nuevaPassword)
        {
            try
            {
                // 1. Buscamos al usuario por ID (asegúrate de que el ID no llegue en 0)
                var usuarioDb = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == model.Id);

                if (usuarioDb == null)
                {
                    return Json(new { success = false, message = "No se encontró el usuario con ID: " + model.Id });
                }

                // 2. Actualizamos los textos básicos
                usuarioDb.NombreCompleto = model.NombreCompleto;
                usuarioDb.Email = model.Email;

                // 3. Contraseña (Sin encriptar, tal cual llega)
                if (!string.IsNullOrEmpty(nuevaPassword))
                {
                    usuarioDb.PasswordHash = nuevaPassword;
                }

                // 4. Procesar la imagen si el usuario subió una nueva
                if (fotoArchivo != null && fotoArchivo.Length > 0)
                {
                    using (var ms = new MemoryStream())
                    {
                        await fotoArchivo.CopyToAsync(ms);
                        usuarioDb.FotoBinaria = ms.ToArray();
                        usuarioDb.ContentType = fotoArchivo.ContentType;
                    }
                }

                // 5. Guardar y confirmar
                _context.Usuarios.Update(usuarioDb);
                await _context.SaveChangesAsync();

                // Actualizar las variables de sesión para que el Navbar sepa el nuevo nombre y foto
                HttpContext.Session.SetString("UsuarioNombre", usuarioDb.NombreCompleto);

                if (usuarioDb.FotoBinaria != null)
                {
                    string fotoBase64 = Convert.ToBase64String(usuarioDb.FotoBinaria);
                    string fotoSrc = $"data:{usuarioDb.ContentType};base64,{fotoBase64}";
                    HttpContext.Session.SetString("UsuarioFoto", fotoSrc);
                }

                return Json(new { success = true, message = "¡Cambios guardados con éxito!" });
            }
            catch (Exception ex)
            {
                // Esto te dirá en la consola si hubo un error de SQL
                return Json(new { success = false, message = "Error interno: " + ex.Message });
            }
        }

        // ================================================
        // 1. GUARDAR (CREAR Y EDITAR - AJAX) - Tu lógica intacta
        [HttpPost]
        public async Task<IActionResult> Guardar(Usuario usuario, IFormFile? fotoArchivo)
        {
            var rolSesion = HttpContext.Session.GetString("RolUsuario");
            var miId = HttpContext.Session.GetInt32("UsuarioId");

            if (rolSesion != "Administrador" && usuario.Id != miId)
                return Json(new { success = false, message = "Sin permisos." });

            ModelState.Remove("Rol");
            ModelState.Remove("Restaurantes");

            try
            {
                Usuario? enBD = usuario.Id > 0 ? await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == usuario.Id) : null;

                // Password
                if (usuario.Id == 0 && string.IsNullOrEmpty(usuario.PasswordHash))
                    return Json(new { success = false, message = "Contraseña obligatoria." });

                if (usuario.Id != 0 && string.IsNullOrWhiteSpace(usuario.PasswordHash))
                    usuario.PasswordHash = enBD?.PasswordHash;

                // Foto
                if (fotoArchivo != null)
                {
                    using var ms = new MemoryStream();
                    await fotoArchivo.CopyToAsync(ms);
                    usuario.FotoBinaria = ms.ToArray();
                    usuario.ContentType = fotoArchivo.ContentType;
                }
                else
                {
                    usuario.FotoBinaria = enBD?.FotoBinaria;
                    usuario.ContentType = enBD?.ContentType;
                }

                if (usuario.Id == 0) _context.Add(usuario);
                else _context.Update(usuario);

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Usuario actualizado." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            if (HttpContext.Session.GetString("RolUsuario") != "Administrador")
                return Json(new { success = false, message = "No autorizado." });

            var u = await _context.Usuarios.FindAsync(id);
            if (u == null) return Json(new { success = false, message = "No existe." });

            _context.Usuarios.Remove(u);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Usuario eliminado." });
        }

        // ================================================
        // 3. REGISTRO PÚBLICO - Tu lógica intacta
        // ================================================

        // MÉTODO 1: Solo para MOSTRAR la página (Petición GET)
        // Esto evita el Error 405 al escribir la URL o refrescar
        // 1. Método para MOSTRAR la página (GET)
        [HttpGet]
        public IActionResult Registrar()
        {
            // Esto limpia cualquier rastro de datos previos para que no se sobreescriban
            ModelState.Clear();
            return View("~/Views/Login/register.cshtml", new Usuario());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(Usuario usuario, string confirmarPassword)
        {
            // 1. Validamos que coincidan
            if (usuario.PasswordHash != confirmarPassword)
            {
                ModelState.AddModelError("", "Las contraseñas no coinciden.");
                return View("~/Views/Login/register.cshtml", usuario);
            }

            // 2. Limpiamos validaciones de navegación
            ModelState.Remove("Rol");
            ModelState.Remove("Restaurantes");

            if (ModelState.IsValid)
            {
                try
                {
                    usuario.RolId = 1; // Cliente por defecto
                    _context.Add(usuario);
                    await _context.SaveChangesAsync();

                    // 3. Guardamos sesión
                    HttpContext.Session.SetInt32("UsuarioId", usuario.Id);

                    // 4. ¡EL AVANCE! Al terminar, el controlador te manda al Selector
                    return RedirectToAction("Selector", "Acceso", new { usuarioId = usuario.Id });
                }
                catch
                {
                    ModelState.AddModelError("", "Error al registrar. Intenta con otro correo.");
                }
            }
            return View("~/Views/Login/register.cshtml", usuario);
        }

        // ================================================
        // 4. UBICACIÓN (MAPAS) - Tu lógica intacta
        // ================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarUbicacion(string Latitud, string Longitud)
        {
            int? userId = HttpContext.Session.GetInt32("UsuarioId");
            if (userId == null) return Json(new { success = false, message = "Sesión expirada" });

            // Usamos InvariantCulture para que el punto decimal siempre se entienda correctamente
            // sin importar si el servidor está en español o inglés.
            bool latOk = decimal.TryParse(Latitud, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal lat);
            bool lngOk = decimal.TryParse(Longitud, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal lng);

            if (latOk && lngOk)
            {
                var usuario = await _context.Usuarios.FindAsync(userId);
                if (usuario != null)
                {
                    usuario.Latitud = lat;
                    usuario.Longitud = lng;

                    await _context.SaveChangesAsync();

                    // Guardamos en sesión como string con PUNTO para que el JS del mapa lo lea directo
                    HttpContext.Session.SetString("Latitud", lat.ToString(CultureInfo.InvariantCulture));
                    HttpContext.Session.SetString("Longitud", lng.ToString(CultureInfo.InvariantCulture));

                    return Json(new { success = true, message = "Ubicación guardada" });
                }
            }
            return Json(new { success = false, message = "Error en formato de coordenadas" });
        }

        [HttpGet]
        public async Task<IActionResult> Movimientos()
        {
            // 1. Validar Sesión: Obtenemos el ID del usuario logueado
            int? usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            if (usuarioId == null)
            {
                // Si no hay sesión, redirigir al Login para evitar errores de acceso
                return RedirectToAction("Login", "Acceso");
            }

            // 2. Consulta con "Eager Loading": Traemos el Pedido y sus hijos
            // .Include(p => p.Detalles) trae la lista de productos comprados
            // .ThenInclude(d => d.Producto) trae los nombres/fotos de esos productos
            var historial = await _context.Pedidos
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .Where(p => p.UsuarioId == usuarioId)
                .OrderByDescending(p => p.FechaHora) // Los más recientes arriba
                .ToListAsync();

            // 3. Retornar la vista específica de movimientos
            return View("~/Views/Navbar/movimientos.cshtml", historial);
        }
    }
}