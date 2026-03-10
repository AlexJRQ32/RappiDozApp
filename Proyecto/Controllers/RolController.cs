using Microsoft.AspNetCore.Mvc;

namespace RappiDozApp.Controllers
{
    public class RolController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
