using Microsoft.EntityFrameworkCore;
using RappiDozApp.Data;
using Microsoft.AspNetCore.Authentication.Cookies; // Necesario para el esquema de autenticación

var builder = WebApplication.CreateBuilder(args);

// 1. CONFIGURACIÓN DE CONTROLADORES Y VISTAS
builder.Services.AddControllersWithViews();

// 2. CONFIGURACIÓN DE BASE DE DATOS
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 3. CONFIGURACIÓN DE AUTENTICACIÓN (Esto soluciona el InvalidOperationException)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Acceso/Login";      // Si no está logueado, va aquí
        options.AccessDeniedPath = "/Home/Index"; // Si no tiene el rol, va aquí
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    });

// 4. CONFIGURACIÓN DE SESIÓN
builder.Services.AddDistributedMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// 5. PIPELINE DE MIDDLEWARE (El orden aquí es CRÍTICO)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// El orden sagrado: Sesión -> Autenticación -> Autorización
app.UseSession();        // Habilita HttpContext.Session
app.UseAuthentication(); // Lee la Cookie y reconoce quién es el usuario
app.UseAuthorization();  // Revisa si tiene permiso ([Authorize])

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();