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
            // Check if the password is correct
            if (password == "pw")
            {
                // Check if the username is "tier2"
                if (username == "t2")
                {
                    // Redirect to Tier 2 dashboard or operations
                    return RedirectToAction("Tier2Dashboard", "Tier2");
                }
                // Check if the username is "t3" for Tier 3
                else if (username == "t3")
                {
                    // Redirect to Tier 3 operations - Approve Deletions
                    return RedirectToAction("Tier3Dashboard", "Tier3");
                }
                else
                {
                    // Invalid username
                    ViewBag.Message = "Invalid username.";
                    return View("~/Views/Home/Login.cshtml");
                }
            }
            else
            {
                // Invalid password
                ViewBag.Message = "Invalid password.";
                return View("~/Views/Home/Login.cshtml");
            }
        }
    }
}
