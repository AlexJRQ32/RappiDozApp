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

        #region Perfil
        [HttpGet]
        public async Task<IActionResult> Perfil()
        {
            int? userId = HttpContext.Session.GetInt32("UsuarioId");
            if (userId == null) return Unauthorized();

            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null) return NotFound();

            return PartialView("~/Views/Usuarios/users-form.cshtml", usuario);
        }

        


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarPerfil(Usuario model, IFormFile? fotoArchivo, string? nuevaPassword)
        {
            try
            {
                var usuarioDb = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == model.Id);

                if (usuarioDb == null)
                {
                    return Json(new { success = false, message = "No se encontró el usuario con ID: " + model.Id });
                }

                usuarioDb.NombreCompleto = model.NombreCompleto;
                usuarioDb.Email = model.Email;

                if (!string.IsNullOrEmpty(nuevaPassword))
                {
                    usuarioDb.PasswordHash = nuevaPassword;
                }

                if (fotoArchivo != null && fotoArchivo.Length > 0)
                {
                    using (var ms = new MemoryStream())
                    {
                        await fotoArchivo.CopyToAsync(ms);
                        usuarioDb.FotoBinaria = ms.ToArray();
                        usuarioDb.ContentType = fotoArchivo.ContentType;
                    }
                }

                _context.Usuarios.Update(usuarioDb);
                await _context.SaveChangesAsync();

                HttpContext.Session.SetString("NombreUsuario", usuarioDb.NombreCompleto);

                return Json(new { success = true, message = "¡Cambios guardados con éxito!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error interno: " + ex.Message });
            }
        }

        #endregion

        #region CRUD
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

                if (usuario.Id == 0 && string.IsNullOrEmpty(usuario.PasswordHash))
                    return Json(new { success = false, message = "Contraseña obligatoria." });

                if (usuario.Id != 0 && string.IsNullOrWhiteSpace(usuario.PasswordHash))
                    usuario.PasswordHash = enBD?.PasswordHash;

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
            try
            {
                var u = await _context.Usuarios.FindAsync(id);
                if (u == null) return Json(new { success = false, message = "Usuario no encontrado." });

                u.Activo = false;

                _context.Usuarios.Update(u);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Usuario ocultado del sistema exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
        #endregion


        #region Foto
        [HttpGet]
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client)]
        public async Task<IActionResult> Foto(int id)
        {
            var data = await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.Id == id)
                .Select(u => new { u.FotoBinaria, u.ContentType })
                .FirstOrDefaultAsync();

            if (data?.FotoBinaria == null || data.FotoBinaria.Length == 0)
                return NotFound();

            return File(data.FotoBinaria, data.ContentType ?? "image/png");
        }
        #endregion
    }
}
