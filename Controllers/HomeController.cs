using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, CsvService csvService)
        {
            _logger = logger;
            _context = context;
            _csvService = csvService;
        }

        // Updated Index Action to handle search functionality
        public IActionResult Index(string searchString)
        {
            var inventoryItems = from item in _context.InventoryItems
                                 select item;

            // If there is a search string, filter the inventory items
            if (!string.IsNullOrEmpty(searchString))
            {
                string lowerCaseSearchString = searchString.ToLower();

                inventoryItems = inventoryItems.Where(i => i.Vendor.ToLower().Contains(lowerCaseSearchString)
                                                        || i.SerialNumber.ToLower().Contains(lowerCaseSearchString));
            }

            return View(inventoryItems.ToList());
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("file", "Please select a valid CSV file.");
                return RedirectToAction("Index");
            }

            if (!Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("file", "Only CSV files are allowed.");
                return RedirectToAction("Index");
            }

            try
            {
                var inventoryItems = new List<InventoryItem>();
                using (var stream = file.OpenReadStream())
                {
                    inventoryItems = _csvService.ReadCsvFile(stream).ToList();
                }

                // Filter out duplicates
                var newItems = new List<InventoryItem>();
                foreach (var item in inventoryItems)
                {
                    if (!_context.InventoryItems.Any(e => e.Id == item.Id))
                    {
                        newItems.Add(item); // Add only new items that do not exist in the database
                    }
                }

                if (newItems.Any())
                {
                    _context.InventoryItems.AddRange(newItems);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"{newItems.Count} new records added successfully.";
                }
                else
                {
                    TempData["InfoMessage"] = "No new records to add.";
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("file", $"An error occurred while processing the file: {ex.Message}");
            }

            return RedirectToAction("Index");
        }

        public IActionResult Privacy()
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
