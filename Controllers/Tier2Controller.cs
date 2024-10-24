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
using System.Globalization;
using Microsoft.AspNetCore.Authorization;

namespace NetTracApp.Controllers
{
    [Authorize(Roles = "Tier2")]
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

            ViewBag.TotalItems = itemList.Count; // Pass the total count to the view
            ViewBag.UserType = "T2";

            return View(itemList);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
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
            int skippedRecords = 0;
            var duplicateRecords = new List<string>();
            var invalidDateRecords = new List<string>();

            foreach (var file in files)
            {
                if (!Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
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
                    var csvConfig = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        TrimOptions = CsvHelper.Configuration.TrimOptions.Trim,
                        HeaderValidated = null,
                        MissingFieldFound = null
                    };

                    using var csv = new CsvHelper.CsvReader(reader, csvConfig);
                    var inventoryItems = csv.GetRecords<InventoryItem>().ToList();

                    foreach (var item in inventoryItems)
                    {
                        // Check if SerialNumber and Vendor are present
                        if (string.IsNullOrWhiteSpace(item.SerialNumber) || string.IsNullOrWhiteSpace(item.Vendor))
                        {
                            skippedRecords++;
                            continue;
                        }

                        // Check if the date is valid
                        if (item.DateReceived == default || item.Modified == default || item.Created == default)
                        {
                            invalidDateRecords.Add(item.SerialNumber);
                            skippedRecords++;
                            continue;
                        }

                        // Check for duplicate SerialNumber
                        bool isDuplicate = await _context.InventoryItems
                            .AnyAsync(e => e.SerialNumber == item.SerialNumber);

                        if (isDuplicate)
                        {
                            duplicateRecords.Add(item.SerialNumber); // Track duplicate SNs
                            continue;
                        }

                        // Add the valid item to the database
                        _context.InventoryItems.Add(item);
                        totalNewRecords++;
                    }

                    await _context.SaveChangesAsync(); // Save all new items at once
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error processing file '{file.FileName}': {ex.Message}";
                }
            }

            // Display messages for skipped and duplicate records
            TempData["SuccessMessage"] = $"{totalNewRecords} new items uploaded successfully.";
            if (skippedRecords > 0)
                TempData["InfoMessage"] = $"{skippedRecords} rows were skipped due to missing fields or invalid data.";
            if (duplicateRecords.Any())
                TempData["DuplicateMessage"] = $"Duplicate Serial Numbers: {string.Join(", ", duplicateRecords)}";
            if (invalidDateRecords.Any())
                TempData["DateErrorMessage"] = $"Invalid Dates found for: {string.Join(", ", invalidDateRecords)}";

            return RedirectToAction(nameof(Tier2Dashboard));
        }




        // POST: Request deletion of selected items
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestDelete(List<string> selectedItems)
        {
            if (selectedItems == null || !selectedItems.Any())
            {
                TempData["InfoMessage"] = "No items selected for deletion.";
                return RedirectToAction("Tier2Dashboard");
            }

            try
            {
                var itemsToUpdate = await _context.InventoryItems
                    .Where(i => selectedItems.Contains(i.SerialNumber))
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
        public IActionResult RequestDelete(string serialNumber)
        {
            var inventoryItem = _context.InventoryItems.FirstOrDefault(i => i.SerialNumber == serialNumber);
            if (inventoryItem == null)
            {
                TempData["ErrorMessage"] = "Item not found.";
                return RedirectToAction(nameof(Tier2Dashboard));
            }

            return View(inventoryItem);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitDeleteRequest(string serialNumber)
        {
            var inventoryItem = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.SerialNumber == serialNumber);

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
        public async Task<IActionResult> Edit(string serialNumber)
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
                return NotFound();

            var item = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.SerialNumber == serialNumber);

            return item == null ? NotFound() : View(item);
        }

        // POST: Edit an inventory item
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string serialNumber, InventoryItem inventoryItem)
        {
            if (serialNumber != inventoryItem.SerialNumber)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Attach the updated item to the context
                    _context.InventoryItems.Update(inventoryItem);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Item updated successfully!";
                    return RedirectToAction(nameof(Tier2Dashboard));
                }
                catch (Exception ex)
                {
                    // Optional: Log the exception for debugging
                    ModelState.AddModelError("", $"There was an error updating the item: {ex.Message}");
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

        // Updated method to check if an InventoryItem exists by SerialNumber
        private bool InventoryItemExists(string serialNumber) =>
            _context.InventoryItems.Any(e => e.SerialNumber == serialNumber);
    }
}
