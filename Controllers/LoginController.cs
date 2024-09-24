using Microsoft.AspNetCore.Mvc;

namespace NetTracApp.Controllers
{
    public class LoginController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            // Specify the path to the Login.cshtml view in the Home folder
            return View("~/Views/Home/Login.cshtml");
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            // Mock authentication logic
            if (password == "pw")
            {
                if (username == "tier2")
                {
                    // Redirect to Tier 2 operations
                    return RedirectToAction("Tier2Dashboard", "Tier2");
                }
                else if (username == "tier3")
                {
                    // Redirect to Tier 3 operations
                    return RedirectToAction("Tier3Dashboard", "Tier3");
                }
                else
                {
                    ViewBag.Message = "Invalid username.";
                    return View("~/Views/Home/Login.cshtml");
                }
            }
            else
            {
                ViewBag.Message = "Invalid password.";
                return View("~/Views/Home/Login.cshtml");
            }
        }
    }
}
