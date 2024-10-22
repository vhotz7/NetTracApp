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

        public Tier2Controller(ApplicationDbContext context, CsvService csvService)
        {
            _context = context;
            _csvService = csvService;
        }

        // GET: Tier2/Tier2Dashboard with search functionality
        public async Task<IActionResult> Tier2Dashboard(string? searchString)
        {
            var items = _context.InventoryItems.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                items = items.Where(i => (i.Vendor ?? string.Empty).Contains(searchString) ||
                                         (i.SerialNumber ?? string.Empty).Contains(searchString));
            }

            var itemList = await items.ToListAsync();
            return View(itemList);
        }


        // POST: Handle CSV file uploads
        [HttpPost]
        public async Task<IActionResult> UploadFile(List<IFormFile> files)
        {
            if (files == null || !files.Any())
            {
                TempData["ErrorMessage"] = "Please select one or more CSV files.";
                return RedirectToAction(nameof(Tier2Dashboard));
            }

            var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

            int totalNewRecords = 0;
            var duplicateRecords = new List<string>();

            foreach (var file in files)
            {
                if (!Path.GetExtension(file.FileName).Equals(".csv", System.StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ErrorMessage"] = "Only CSV files are allowed.";
                    continue;
                }

                var savedFilePath = Path.Combine(uploadFolder, file.FileName);
                using (var stream = new FileStream(savedFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                try
                {
                    using var reader = new StreamReader(savedFilePath);
                    var inventoryItems = _csvService.ReadCsvFile(reader.BaseStream).ToList();

                    foreach (var item in inventoryItems)

                    {
                        if (item?.SerialNumber != null) // Check if item and SerialNumber are not null
                        {
                            if (_context.InventoryItems.Any(e => e.SerialNumber == item.SerialNumber))
                                duplicateRecords.Add(item.SerialNumber);
                            else
                            {
                                _context.InventoryItems.Add(item);
                                totalNewRecords++;
                            }
                        }

                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error processing file '{file.FileName}': {ex.Message}";
                }
            }

            TempData["SuccessMessage"] = $"{totalNewRecords} new items uploaded successfully.";
            if (duplicateRecords.Any())
                TempData["DuplicateMessage"] = $"Duplicates: {string.Join(", ", duplicateRecords)}";

            return RedirectToAction(nameof(Tier2Dashboard));
        }

        // POST: Request deletion of selected items
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestDelete(List<int> selectedItems)
        {
            if (selectedItems == null || !selectedItems.Any())
            {
                TempData["InfoMessage"] = "No items selected for deletion.";
                return RedirectToAction("Tier2Dashboard");
            }

            try
            {
                var itemsToUpdate = await _context.InventoryItems
                    .Where(i => selectedItems.Contains(i.Id))
                    .ToListAsync();

                foreach (var item in itemsToUpdate)
                {
                    item.PendingDeletion = true;
                    item.DeletionApproved = false;
                }

                _context.InventoryItems.UpdateRange(itemsToUpdate);
                await _context.SaveChangesAsync();

                TempData["InfoMessage"] = "Selected items marked for deletion and sent to Tier 3 for approval.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RequestDelete: {ex.Message}");
                TempData["ErrorMessage"] = "Failed to request deletion.";
            }

            return RedirectToAction("Tier2Dashboard");
        }
        // GET: Display the delete request confirmation page for Tier 2
        // GET: Display the Request Delete page
        [HttpGet]
        public IActionResult RequestDelete(int id)
        {
            var inventoryItem = _context.InventoryItems.FirstOrDefault(i => i.Id == id);
            if (inventoryItem == null)
            {
                TempData["ErrorMessage"] = "Item not found.";
                return RedirectToAction(nameof(Tier2Dashboard));
            }

            return View(inventoryItem); // Render the RequestDelete view
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitDeleteRequest(int id)
        {
            var inventoryItem = await _context.InventoryItems.FindAsync(id);

            if (inventoryItem != null)
            {
                // Mark item as pending deletion for Tier 3 approval
                inventoryItem.PendingDeletion = true;
                _context.Update(inventoryItem);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Deletion request submitted for Tier 3 approval.";
            }
            else
            {
                TempData["ErrorMessage"] = "Item not found.";
            }

            return RedirectToAction(nameof(Tier2Dashboard)); // Redirect to Tier2 dashboard
        }




        // POST: Save inventory items to a new CSV file
        [HttpPost]
        public IActionResult SaveAsNewFile()
        {
            try
            {
                var inventoryItems = _context.InventoryItems.ToList();
                var memoryStream = new MemoryStream();

                using (var writer = new StreamWriter(memoryStream, leaveOpen: true))
                using (var csv = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(inventoryItems);
                }

                memoryStream.Position = 0;
                return File(memoryStream, "text/csv", $"UpdatedInventory_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error generating the file: {ex.Message}";
                return RedirectToAction(nameof(Tier2Dashboard));
            }
        }

        // GET: Create new inventory item
        public IActionResult Create() => View();

        // POST: Create a new inventory item
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Vendor,DeviceType,SerialNumber,HostName,AssetTag,PartID,FutureLocation,DateReceived,CurrentLocation,Status,BackOrdered,Notes,ProductDescription,Ready,LegacyDevice")] InventoryItem inventoryItem)
        {
            if (ModelState.IsValid)
            {
                _context.Add(inventoryItem);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Item created successfully!";
                return RedirectToAction(nameof(Tier2Dashboard));
            }
            return View(inventoryItem);
        }

        // GET: Edit an inventory item
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var item = await _context.InventoryItems.FindAsync(id);
            return item == null ? NotFound() : View(item);
        }

        // POST: Edit an inventory item
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, InventoryItem inventoryItem)
        {
            if (id != inventoryItem.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(inventoryItem);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Tier2Dashboard));
                }
                catch
                {
                    return Problem("There was an error updating the item.");
                }
            }

            return View(inventoryItem);
        }

        // GET: Tier2/Index (default view)
        public async Task<IActionResult> Index()
        {
            var items = await _context.InventoryItems.ToListAsync();
            return View(items);
        }

        private bool InventoryItemExists(int id) => _context.InventoryItems.Any(e => e.Id == id);
    }
}
