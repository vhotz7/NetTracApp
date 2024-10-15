using Microsoft.AspNetCore.Mvc;
using NetTracApp.Data;
using NetTracApp.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.IO;
using NetTracApp.Services;

namespace NetTracApp.Controllers
{
    public class Tier3Controller : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CsvService _csvService;

        // Injecting ApplicationDbContext and CsvService into the controller
        public Tier3Controller(ApplicationDbContext context, CsvService csvService)
        {
            _context = context;
            _csvService = csvService;
        }

        // Displays items awaiting deletion approval and current inventory
        public IActionResult ApproveDeletions()
        {
            var inventoryItems = _context.InventoryItems.ToList();
            return View(inventoryItems);
        }

        // Handles approval of deletions
        [HttpPost]
        public async Task<IActionResult> ApproveDeletion(int id)
        {
            var inventoryItem = await _context.InventoryItems.FindAsync(id);

            if (inventoryItem == null)
            {
                return NotFound();
            }

            _context.InventoryItems.Remove(inventoryItem);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Item deletion approved and item removed from inventory.";
            return RedirectToAction(nameof(ApproveDeletions));
        }

        // Handles direct deletion of items (for Tier 3 users)
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var inventoryItem = await _context.InventoryItems.FindAsync(id);

            if (inventoryItem == null)
            {
                return NotFound();
            }

            _context.InventoryItems.Remove(inventoryItem);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Item deleted successfully.";
            return RedirectToAction(nameof(ApproveDeletions));
        }

        // Handles file upload for CSV files
        [HttpPost]
        public async Task<IActionResult> UploadFile(List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
            {
                ModelState.AddModelError("files", "Please select one or more CSV files.");
                return RedirectToAction(nameof(ApproveDeletions));
            }

            var totalNewRecords = 0;

            foreach (var file in files)
            {
                if (!Path.GetExtension(file.FileName).Equals(".csv", System.StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError("files", "Only CSV files are allowed.");
                    continue;
                }

                try
                {
                    var inventoryItems = new List<InventoryItem>();
                    using (var stream = file.OpenReadStream())
                    {
                        // Correct call to your custom CsvService
                        inventoryItems = _csvService.ReadCsvFile(stream).ToList();
                    }

                    var newItems = new List<InventoryItem>();
                    foreach (var item in inventoryItems)
                    {
                        if (!_context.InventoryItems.Any(e => e.SerialNumber == item.SerialNumber))
                        {
                            newItems.Add(item);
                        }
                    }

                    if (newItems.Any())
                    {
                        _context.InventoryItems.AddRange(newItems);
                        await _context.SaveChangesAsync();
                        totalNewRecords += newItems.Count;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("files", $"Error processing file '{file.FileName}': {ex.Message}");
                }
            }

            if (totalNewRecords > 0)
            {
                TempData["SuccessMessage"] = $"{totalNewRecords} new records added successfully.";
            }
            else
            {
                TempData["InfoMessage"] = "No new records to add from any of the files.";
            }

            return RedirectToAction(nameof(ApproveDeletions));
        }
    }
}
