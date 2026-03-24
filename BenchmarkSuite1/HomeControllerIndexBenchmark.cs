using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RappiDozApp.Controllers;
using RappiDozApp.Data;
using RappiDozApp.Models;
using Microsoft.VSDiagnostics;

namespace RappiDozApp.Benchmarks;
[CPUUsageDiagnoser]
public class HomeControllerIndexBenchmark
{
    private ApplicationDbContext _context = null!;
    private DefaultHttpContext _httpContext = null!;
    [GlobalSetup]
    public void GlobalSetup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=HomeControllerIndexBenchmarkDb;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        _context = new ApplicationDbContext(options);
        _context.Database.EnsureDeleted();
        _context.Database.EnsureCreated();
        SeedData();
        _httpContext = new DefaultHttpContext();
        _httpContext.Session = new TestSession();
        _httpContext.Session.SetInt32("UsuarioId", 1);
    }

    [Benchmark]
    public async Task<IActionResult> Index()
    {
        var controller = new HomeController(_context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            }
        };
        return await controller.Index();
    }

    private void SeedData()
    {
        _context.Usuarios.Add(new Usuario { Id = 1, NombreCompleto = "Usuario Benchmark", Email = "bench@example.com", PasswordHash = "hash", RolId = 1 });
        _context.Categorias.Add(new Categoria { Id = 1, Nombre = "Pizza" });
        _context.UbicacionUsuario.Add(new UbicacionUsuario { IdUbicacion = 1, IdUsuario = 1, NombreUbicacion = "Casa", Latitud = 9.9331m, Longitud = -84.1112m, EsPrincipal = true });
        var ahora = DateTime.Now.TimeOfDay;
        var restaurantes = Enumerable.Range(1, 200).Select(i => new Restaurante { Id = i, NombreComercial = $"Restaurante {i}", Direccion = $"Dirección {i}", CategoriaId = 1, HoraApertura = ahora.Subtract(TimeSpan.FromHours(1)), HoraCierre = ahora.Add(TimeSpan.FromHours(8)), UsuarioId = 1, ContentType = "image/png", LogoBinario = new byte[4096] }).ToList();
        _context.Restaurantes.AddRange(restaurantes);
        _context.SaveChanges();
    }

    private sealed class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();
        public IEnumerable<string> Keys => _store.Keys;
        public string Id { get; } = Guid.NewGuid().ToString();
        public bool IsAvailable => true;

        public void Clear() => _store.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, out byte[]? value) => _store.TryGetValue(key, out value);
    }
}