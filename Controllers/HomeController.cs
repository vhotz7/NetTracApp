using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using NetTracApp.Data;
using NetTracApp.Models;
using NetTracApp.Services;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace NetTracApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        [Authorize] // Ensure only authenticated users access this action
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                // Check the user role and redirect accordingly
                if (await _userManager.IsInRoleAsync(user, "Tier2"))
                {
                    return RedirectToAction("Tier2Dashboard", "Tier2");
                }
                else if (await _userManager.IsInRoleAsync(user, "Tier3"))
                {
                    return RedirectToAction("ApproveDeletions", "Tier3");
                }
            }

            // Redirect to login if no user is found
            return RedirectToAction("Index", "Login");
        }


        public IActionResult AboutUs()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
