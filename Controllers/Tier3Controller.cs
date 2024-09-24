using Microsoft.AspNetCore.Mvc;

namespace NetTracApp.Controllers
{
    public class Tier3Controller : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
