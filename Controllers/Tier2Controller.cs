using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTracApp.Data;
using NetTracApp.Models;
using NetTracApp.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NetTracApp.Controllers
{
    public class Tier2Controller : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CsvService _csvService;

        // Injecting ApplicationDbContext and CsvService into the controller
        public Tier2Controller(ApplicationDbContext context, CsvService csvService)
        {
            _context = context;
            _csvService = csvService;
        }

        public async Task<IActionResult> Tier2Dashboard(string? searchString)
        {
            // Start with all items in the inventory
            var items = _context.InventoryItems.AsQueryable();

            // Ensure searchString isn't null or empty, and filter items if needed
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                items = items.Where(i => (i.Vendor ?? string.Empty).Contains(searchString) ||
                                         (i.SerialNumber ?? string.Empty).Contains(searchString));
            }

            // Fetch and return the filtered or unfiltered list of items
            var itemList = await items.ToListAsync();

            return View(itemList);
        }


        // POST: Handle bulk upload of inventory items from CSV files
        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> UploadFile(List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
            {
                ModelState.AddModelError("files", "Please select one or more CSV files.");
                return RedirectToAction(nameof(Tier2Dashboard));
            }

            var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            // Ensure the uploads folder exists
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            var totalNewRecords = 0;
            var duplicateRecords = new List<string>();
            string savedFilePath = "";

            foreach (var file in files)
            {
                if (!Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError("files", "Only CSV files are allowed.");
                    continue;
                }

                // Construct the full path to save the file
                savedFilePath = Path.Combine(uploadFolder, file.FileName);

                // Save the uploaded file
                using (var stream = new FileStream(savedFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                try
                {
                    var inventoryItems = new List<InventoryItem>();
                    using (var reader = new StreamReader(savedFilePath))
                    {
                        inventoryItems = _csvService.ReadCsvFile(reader.BaseStream).ToList();
                    }

                    foreach (var item in inventoryItems)
                    {
                        if (_context.InventoryItems.Any(e => e.SerialNumber == item.SerialNumber))
                        {
                            duplicateRecords.Add(item.SerialNumber);
                        }
                        else
                        {
                            _context.InventoryItems.Add(item);
                            totalNewRecords++;
                        }
                    }

                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("files", $"Error processing file '{file.FileName}': {ex.Message}");
                }
            }

            TempData["SuccessMessage"] = $"{totalNewRecords} new items uploaded successfully.";
            TempData["DuplicateMessage"] = duplicateRecords.Any()
                ? $"Duplicates found: {string.Join(", ", duplicateRecords)}"
                : "No duplicates found.";

            TempData["UploadedFilePath"] = savedFilePath;

            return RedirectToAction(nameof(Tier2Dashboard));
        }
        // GET: /Tier2/Tier2Dashboard
        


        [HttpPost]
        public IActionResult SaveAsNewFile()
        {
            try
            {
                // Retrieve the latest inventory data from the database
                var inventoryItems = _context.InventoryItems.ToList();

                // Create a MemoryStream to hold the CSV data
                var memoryStream = new MemoryStream();

                // Write CSV data to the memory stream
                using (var writer = new StreamWriter(memoryStream, leaveOpen: true))
                using (var csv = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(inventoryItems);
                }

                // Reset the stream position to the beginning
                memoryStream.Position = 0;

                // Return the CSV as a downloadable file
                return File(
                    memoryStream,
                    "text/csv",
                    $"UpdatedInventory_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                );
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error generating the new file: {ex.Message}";
                return RedirectToAction(nameof(Tier2Dashboard));
            }
        }




        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var inventoryItem = await _context.InventoryItems.FindAsync(id);

            if (inventoryItem == null)
            {
                return NotFound();
            }

            if (User.IsInRole("Tier3"))
            {
                _context.InventoryItems.Remove(inventoryItem);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Item deleted successfully.";
                return RedirectToAction(nameof(Tier2Dashboard));
            }
            else
            {
                inventoryItem.PendingDeletion = true;
                inventoryItem.DeletionApproved = false;
                _context.InventoryItems.Update(inventoryItem);
                await _context.SaveChangesAsync();
                TempData["InfoMessage"] = "Item deletion is waiting for Tier 3 approval.";
                return RedirectToAction(nameof(Tier2Dashboard));
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSelected(List<int> selectedIds)
        {
            if (selectedIds == null || !selectedIds.Any())
            {
                TempData["InfoMessage"] = "No items selected for deletion.";
                return RedirectToAction(nameof(Tier2Dashboard));
            }

            try
            {
                // Retrieve the selected items from the database
                var itemsToDelete = await _context.InventoryItems
                    .Where(item => selectedIds.Contains(item.Id))
                    .ToListAsync();

                if (itemsToDelete.Any())
                {
                    if (User.IsInRole("Tier3"))
                    {
                        // Directly delete the items if the user is Tier 3
                        _context.InventoryItems.RemoveRange(itemsToDelete);
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = $"{itemsToDelete.Count} items deleted successfully.";
                    }
                    else
                    {
                        // Mark the items as pending deletion for Tier 3 approval
                        foreach (var item in itemsToDelete)
                        {
                            item.PendingDeletion = true;
                            item.DeletionApproved = false; // Ensure it's not yet approved
                        }

                        // Update the items in the database
                        _context.InventoryItems.UpdateRange(itemsToDelete);
                        await _context.SaveChangesAsync(); // Save the changes

                        TempData["InfoMessage"] = "Selected items are marked for deletion and are awaiting Tier 3 approval.";
                    }
                }
                else
                {
                    TempData["InfoMessage"] = "No valid items found for deletion.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting items: {ex.Message}";
            }

            return RedirectToAction(nameof(Tier2Dashboard));
        }





        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAll()
        {
            var items = _context.InventoryItems.ToList();

            if (User.IsInRole("Tier3"))
            {
                _context.InventoryItems.RemoveRange(items);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "All inventory items deleted successfully.";
            }
            else
            {
                foreach (var item in items)
                {
                    item.PendingDeletion = true;
                    item.DeletionApproved = false;
                }
                _context.InventoryItems.UpdateRange(items);
                await _context.SaveChangesAsync();
                TempData["InfoMessage"] = "All items are marked for deletion and are awaiting Tier 3 approval.";
            }

            return RedirectToAction(nameof(Tier2Dashboard));
        }

        private bool InventoryItemExists(int id)
        {
            return _context.InventoryItems.Any(e => e.Id == id);
        }
    }
}
