using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RappiDozApp.Data;
using RappiDozApp.Models;
using System.Globalization;

namespace RappiDozApp.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsuariosController(ApplicationDbContext context)
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

        
    }
}
