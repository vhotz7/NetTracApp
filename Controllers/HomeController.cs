using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity; // Include for UserManager
using NetTracApp.Data;
using NetTracApp.Models;
using NetTracApp.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NetTracApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly CsvService _csvService;
        private readonly UserManager<IdentityUser> _userManager; // Add UserManager

        // Constructor to inject dependencies
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, CsvService csvService, UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _context = context;
            _csvService = csvService;
            _userManager = userManager; // Initialize UserManager
        }

        public async Task<IActionResult> Index(string searchString)
        {
            // Check if the user is logged in
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                // Check user roles and redirect accordingly
                if (User.IsInRole("Tier2"))
                {
                    return RedirectToAction("Tier2Dashboard", "Tier2"); // Redirect to Tier 2 Dashboard
                }
                else if (User.IsInRole("Tier3"))
                {
                    return RedirectToAction("ApproveDeletions", "Tier3"); // Redirect to Tier 3 Dashboard
                }
            }

            // Default behavior if user is not logged in
            var inventoryItems = from item in _context.InventoryItems
                                 select item;

            if (!string.IsNullOrEmpty(searchString))
            {
                string lowerCaseSearchString = searchString.ToLower();
                inventoryItems = inventoryItems.Where(i => i.Vendor.ToLower().Contains(lowerCaseSearchString)
                                                        || i.SerialNumber.ToLower().Contains(lowerCaseSearchString));
            }

            return View(inventoryItems.ToList());
        }

        public IActionResult AboutUs()
        {
            return View();
        }

        // action to handle error pages
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
   




