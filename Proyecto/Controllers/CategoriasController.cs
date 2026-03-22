using Microsoft.AspNetCore.Mvc;

namespace RappiDozApp.Controllers
{
    public class CategoriasController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

