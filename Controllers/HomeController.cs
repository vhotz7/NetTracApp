using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NetTracApp.Data;
using NetTracApp.Models;
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

        // Constructor that injects the ILogger and ApplicationDbContext
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // Index action to display a list of inventory items
        public IActionResult Index()
        {
            // Retrieve all inventory items from the database
            var inventoryItems = _context.InventoryItems.ToList();

            // Pass the list of inventory items to the view
            return View(inventoryItems);
        }

        // Action method to handle the file upload
        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            // Check if a file was uploaded
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("file", "Please select a valid CSV file.");
                return RedirectToAction("Index");
            }

            // Validate file extension
            if (!Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("file", "Only CSV files are allowed.");
                return RedirectToAction("Index");
            }

            // Define the path to save the uploaded file (adjust path as needed)
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", file.FileName);

            // Create the directory if it doesn't exist
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            // Save the file to the specified path
            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Process the uploaded CSV file
            var inventoryItems = new List<InventoryItem>();
            using (var reader = new StreamReader(path))
            {
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    var values = line.Split(',');

                    if (values.Length == 10) // Ensure this matches the number of expected columns
                    {
                        var item = new InventoryItem
                        {
                            Vendor = values[0],
                            DeviceType = values[1],
                            SerialNumber = values[2],
                            HostName = values[3],
                            AssetTag = values[4],
                            PartId = values[5],
                            FutureLocation = values[6],
                            DateReceived = DateTime.TryParse(values[7], out var dateReceived) ? dateReceived : DateTime.Now, // Handle date parsing
                            CurrentLocation = values[8],
                        };

                        // Convert Status string to InventoryStatus enum
                        if (Enum.TryParse<InventoryStatus>(values[9], true, out var status))
                        {
                            item.Status = status;
                        }
                        else
                        {
                            // Handle invalid status value, e.g., set a default or skip this record
                            ModelState.AddModelError("file", $"Invalid status value: {values[9]}");
                            continue; // Skip this record or handle as needed
                        }

                        inventoryItems.Add(item);
                    }
                }
            }

            // Save the inventory items to the database
            _context.InventoryItems.AddRange(inventoryItems);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // Privacy action (standard page, might contain information about privacy policies)
        public IActionResult Privacy()
        {
            return View();
        }

        // Error action to handle errors and display an error view
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
