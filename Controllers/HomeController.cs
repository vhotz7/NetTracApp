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
        // dependencies for logging, database context, and CSV service
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly CsvService _csvService;

        // constructor to inject dependencies
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, CsvService csvService)
        {
            _logger = logger;
            _context = context;
            _csvService = csvService;
        }

        // index action to display the main page and handle search functionality
        public IActionResult Index(string searchString)
        {
            // query all inventory items from the database
            var inventoryItems = from item in _context.InventoryItems
                                 select item;

            // filter inventory items based on the search string
            if (!string.IsNullOrEmpty(searchString))
            {
                string lowerCaseSearchString = searchString.ToLower();

                inventoryItems = inventoryItems.Where(i => i.Vendor.ToLower().Contains(lowerCaseSearchString)
                                                        || i.SerialNumber.ToLower().Contains(lowerCaseSearchString));
            }

            // return the filtered list to the view
            return View(inventoryItems.ToList());
        }

        // action to handle CSV file upload
        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            // check if the file is null or empty
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("file", "Please select a valid CSV file.");
                return RedirectToAction("Index");
            }

            // check if the file has a .csv extension
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
                    // read the CSV file and convert it to a list of inventory items
                    inventoryItems = _csvService.ReadCsvFile(stream).ToList();
                }

                // filter out duplicate records
                var newItems = new List<InventoryItem>();
                foreach (var item in inventoryItems)
                {
                    if (!_context.InventoryItems.Any(e => e.Id == item.Id))
                    {
                        newItems.Add(item); // add only new items that do not exist in the database
                    }
                }

                // if new records are found, add them to the database
                if (newItems.Any())
                {
                    _context.InventoryItems.AddRange(newItems);
                    await _context.SaveChangesAsync(); // save changes to the database
                    TempData["SuccessMessage"] = $"{newItems.Count} new records added successfully.";
                }
                else
                {
                    TempData["InfoMessage"] = "No new records to add.";
                }
            }
            catch (Exception ex)
            {
                // handle errors during file processing
                ModelState.AddModelError("file", $"An error occurred while processing the file: {ex.Message}");
            }

            // redirect back to the main page
            return RedirectToAction("Index");
        }

        // action to display the privacy page
        public IActionResult Privacy()
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
