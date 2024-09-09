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

        public IActionResult Index()
        {
            var inventoryItems = _context.InventoryItems.ToList();
            return View(inventoryItems);
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

            var inventoryItems = new List<InventoryItem>();
            using (var stream = file.OpenReadStream())
            {
                inventoryItems = _csvService.ReadCsvFile(stream).ToList();
            }

            if (inventoryItems.Any())
            {
                _context.InventoryItems.AddRange(inventoryItems);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("file", "No valid records found in the CSV file.");
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
