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
using CsvHelper.Configuration;
using CsvHelper;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;

namespace NetTracApp.Controllers
{
    [Authorize(Roles = "Tier3")]
    public class Tier3Controller : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CsvService _csvService;

        public Tier3Controller(ApplicationDbContext context, CsvService csvService)
        {
            _context = context;
            _csvService = csvService;  // Initialize csvService
        }





        private async Task<int> GetPendingDeletionsCount()
        {
            return await _context.InventoryItems
                .Where(i => i.PendingDeletion && !i.DeletionApproved)
                .CountAsync();

        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFile(List<IFormFile> files)
        {
            if (files == null || !files.Any())
            {
                TempData["ErrorMessage"] = "Please select one or more CSV files.";
                return RedirectToAction(nameof(Tier3Dashboard));
            }

            var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

            int totalNewRecords = 0;
            int skippedRecords = 0;
            var duplicateSerialNumbers = new List<string>();
            var uniqueItems = new HashSet<string>(); // Track unique rows to prevent duplicates within the CSV

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
                        // Check for duplicate entries within the same CSV based on SerialNumber
                        if (!uniqueItems.Add(item.SerialNumber))
                        {
                            skippedRecords++; // Skip duplicate rows in the CSV itself
                            continue;
                        }

                        // Validate required fields
                        if (string.IsNullOrWhiteSpace(item.SerialNumber) || string.IsNullOrWhiteSpace(item.Vendor))
                        {
                            skippedRecords++; // Skip rows with missing fields
                            continue;
                        }

                        // Validate dates; leave blank if invalid
                        if (!DateTime.TryParse(item.DateReceived.ToString(), out DateTime dateReceived))
                            item.DateReceived = null;

                        if (!DateTime.TryParse(item.Created.ToString(), out DateTime created))
                            item.Created = DateTime.Now;

                        if (!DateTime.TryParse(item.Modified.ToString(), out DateTime modified))
                            item.Modified = DateTime.Now;

                        // Check for duplicates in the database
                        bool isDuplicateInDb = await _context.InventoryItems
                            .AnyAsync(e => e.SerialNumber == item.SerialNumber);

                        if (isDuplicateInDb)
                        {
                            duplicateSerialNumbers.Add(item.SerialNumber);
                            continue;
                        }

                        // Add valid item to the database
                        _context.InventoryItems.Add(item);
                        totalNewRecords++;
                    }

                    await _context.SaveChangesAsync(); // Save all new items
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error processing file '{file.FileName}': {ex.Message}";
                }
            }

            // Display results
            TempData["SuccessMessage"] = $"{totalNewRecords} new items uploaded successfully.";
            if (skippedRecords > 0)
                TempData["InfoMessage"] = $"{skippedRecords} rows were skipped due to duplicates or missing data.";
            if (duplicateSerialNumbers.Any())
                TempData["DuplicateMessage"] = $"Duplicate Serial Numbers: {string.Join(", ", duplicateSerialNumbers)}";

            return RedirectToAction(nameof(Tier3Dashboard));
        }





        public async Task<IActionResult> Tier3Dashboard(string? searchString)
        {
            // Get pending deletions count
            int pendingDeletionsCount = await GetPendingDeletionsCount();

            // Retrieve inventory items with optional search filtering
            var itemsQuery = _context.InventoryItems.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                itemsQuery = itemsQuery.Where(i =>
                    (i.Vendor ?? string.Empty).Contains(searchString) ||
                    (i.SerialNumber ?? string.Empty).Contains(searchString));
            }

            var items = await itemsQuery.ToListAsync(); // Execute query and get the items
            ViewBag.TotalItems = items.Count; // Pass total item count to the view
            ViewBag.UserType = "T3";

            // Use ViewData to pass additional data to the view
            ViewData["PendingDeletionsCount"] = pendingDeletionsCount;
            ViewData["searchString"] = searchString;

            return View(items); // Pass the item list to the view
        }



        // GET: Pending Deletions Page
        public async Task<IActionResult> PendingDeletions()
        {
            var pendingItems = await _context.InventoryItems
                .Where(i => i.PendingDeletion && !i.DeletionApproved)
                .ToListAsync();

            return View(pendingItems);
        }

        // POST: Approve a pending deletion request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveDeletion(string serialNumber)
        {
            var item = await _context.InventoryItems.FindAsync(serialNumber); // Use serialNumber as key
            if (item == null) return NotFound();

            _context.InventoryItems.Remove(item);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Item deletion approved.";
            return RedirectToAction(nameof(PendingDeletions));
        }


        // POST: Deny a pending deletion request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DenyDeletion(string serialNumber)
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
            {
                TempData["ErrorMessage"] = "Invalid serial number.";
                return RedirectToAction(nameof(PendingDeletions));
            }

            var item = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.SerialNumber == serialNumber);

            if (item == null)
            {
                TempData["ErrorMessage"] = "Item not found.";
                return RedirectToAction(nameof(PendingDeletions));
            }

            item.PendingDeletion = false;  // Reset pending status
            _context.InventoryItems.Update(item);
            await _context.SaveChangesAsync();

            TempData["InfoMessage"] = "Item deletion denied.";
            return RedirectToAction(nameof(PendingDeletions));
        }


        // GET: Display the delete confirmation page
        [HttpGet]
        public async Task<IActionResult> Delete(string serialNumber)
        {
            var inventoryItem = await _context.InventoryItems.FindAsync(serialNumber);
            if (inventoryItem == null)
            {
                TempData["ErrorMessage"] = "Item not found.";
                return RedirectToAction(nameof(Tier3Dashboard));
            }

            return View(inventoryItem); // Render the delete confirmation page
        }


        // POST: Handle the deletion after confirmation
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string serialNumber)
        {
            var inventoryItem = await _context.InventoryItems.FindAsync(serialNumber);

            if (inventoryItem != null)
            {
                _context.InventoryItems.Remove(inventoryItem);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Item deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Item not found.";
            }

            return RedirectToAction(nameof(Tier3Dashboard));
        }






        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSelected(string[] selectedSerialNumbers)
        {
            if (selectedSerialNumbers == null || !selectedSerialNumbers.Any())
            {
                TempData["ErrorMessage"] = "No items selected for deletion.";
                return RedirectToAction(nameof(Tier3Dashboard));
            }

            var itemsToDelete = await _context.InventoryItems
                .Where(item => selectedSerialNumbers.Contains(item.SerialNumber))
                .ToListAsync();

            if (!itemsToDelete.Any())
            {
                TempData["ErrorMessage"] = "No matching items found.";
                return RedirectToAction(nameof(Tier3Dashboard));
            }

            _context.InventoryItems.RemoveRange(itemsToDelete);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{itemsToDelete.Count} items deleted successfully.";
            return RedirectToAction(nameof(Tier3Dashboard));
        }





        [HttpPost]
        public IActionResult ApproveAllDeletions()
        {
            // Retrieve all items marked for pending deletion
            var pendingItems = _context.InventoryItems
                                       .Where(item => item.PendingDeletion)
                                       .ToList();

            if (pendingItems.Any())
            {
                // Remove all pending items from the database
                _context.InventoryItems.RemoveRange(pendingItems);

                // Save the changes to the database
                _context.SaveChanges();

                // Set success message for the user
                TempData["SuccessMessage"] = "All pending deletion requests have been approved and items removed.";
            }
            else
            {
                // Inform the user that there are no items to approve
                TempData["InfoMessage"] = "No items are pending for deletion.";
            }

            // Redirect to the Tier 3 Dashboard
            return RedirectToAction("Tier3Dashboard");
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
                return RedirectToAction(nameof(Tier3Dashboard));
            }
            return View(inventoryItem);
        }


        // GET: Edit an item in Tier3Dashboard
        public async Task<IActionResult> Edit(string serialNumber)
        {
            if (string.IsNullOrEmpty(serialNumber))
                return NotFound();

            var item = await _context.InventoryItems.FindAsync(serialNumber);
            return item == null ? NotFound() : View(item);
        }

        // POST: Update item details
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string serialNumber, [Bind("SerialNumber,Vendor,DeviceType,HostName,AssetTag,PartID,FutureLocation,DateReceived,CurrentLocation,Status,BackOrdered,Notes,ProductDescription,Ready,LegacyDevice,CreatedBy,ModifiedBy,PendingDeletion")] InventoryItem inventoryItem)
        {
            if (serialNumber != inventoryItem.SerialNumber)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(inventoryItem);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Item updated successfully!";
                    return RedirectToAction(nameof(Tier3Dashboard));
                }
                catch (Exception ex)
                {
                    // Optional: Log the exception
                    ModelState.AddModelError(string.Empty, $"Error updating the item: {ex.Message}");
                }
            }
            return View(inventoryItem);
        }




    }

}



