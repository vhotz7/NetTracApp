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

        // POST: Upload CSV File
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFile(List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
            {
                TempData["ErrorMessage"] = "Please select at least one CSV file.";
                return RedirectToAction(nameof(Tier3Dashboard));
            }

            var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            // Ensure the upload folder exists
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            int totalNewRecords = 0;
            var duplicateRecords = new List<string>();

            foreach (var file in files)
            {
                if (Path.GetExtension(file.FileName).ToLower() != ".csv")
                {
                    TempData["ErrorMessage"] = "Only CSV files are allowed.";
                    continue;
                }

                // Save the uploaded file
                var savedFilePath = Path.Combine(uploadFolder, file.FileName);
                using (var stream = new FileStream(savedFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                try
                {
                    // Process the CSV file
                    using var reader = new StreamReader(savedFilePath);
                    var inventoryItems = _csvService.ReadCsvFile(reader.BaseStream).ToList();

                    foreach (var item in inventoryItems)
                    {
                        // Check for duplicates based on SerialNumber
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

                    // Save changes to the database
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error processing file '{file.FileName}': {ex.Message}";
                }
            }

            // Display feedback to the user
            TempData["SuccessMessage"] = $"{totalNewRecords} new items uploaded successfully.";
            if (duplicateRecords.Any())
            {
                TempData["DuplicateMessage"] = $"Duplicate items: {string.Join(", ", duplicateRecords)}";
            }

            return RedirectToAction(nameof(Tier3Dashboard));
        }
        // GET: Tier3 Dashboard (like Tier2Dashboard)
        // GET: Tier3Dashboard
        public async Task<IActionResult> Tier3Dashboard(string? searchString)
        {
            // Get pending deletions count
            int pendingDeletionsCount = await GetPendingDeletionsCount();

            // Retrieve inventory items with optional search filtering
            var items = _context.InventoryItems.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                items = items.Where(i =>
                    (i.Vendor ?? string.Empty).Contains(searchString) ||
                    (i.SerialNumber ?? string.Empty).Contains(searchString));
            }

            var itemList = await items.ToListAsync();

            // Use ViewData to pass pending deletions count to the view
            ViewData["PendingDeletionsCount"] = pendingDeletionsCount;
            ViewData["searchString"] = searchString;

            return View(itemList);
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
        public async Task<IActionResult> ApproveDeletion(int id)
        {
            var item = await _context.InventoryItems.FindAsync(id);
            if (item == null) return NotFound();

            _context.InventoryItems.Remove(item);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Item deletion approved.";
            return RedirectToAction(nameof(PendingDeletions));
        }

        // POST: Deny a pending deletion request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DenyDeletion(int id)
        {
            var item = await _context.InventoryItems.FindAsync(id);
            if (item == null) return NotFound();

            item.PendingDeletion = false;  // Reset pending status
            _context.InventoryItems.Update(item);
            await _context.SaveChangesAsync();

            TempData["InfoMessage"] = "Item deletion denied.";
            return RedirectToAction(nameof(PendingDeletions));
        }

        // POST: Delete an item directly from Tier3Dashboard
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.InventoryItems.FindAsync(id);
            if (item == null) return NotFound();

            _context.InventoryItems.Remove(item);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Item deleted successfully.";
            return RedirectToAction(nameof(Tier3Dashboard));
        }

        // POST: Bulk delete selected items from Tier3Dashboard
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSelected(List<int> selectedIds)
        {
            if (selectedIds == null || !selectedIds.Any())
            {
                TempData["InfoMessage"] = "No items selected.";
                return RedirectToAction(nameof(Tier3Dashboard));
            }

            var itemsToDelete = await _context.InventoryItems
                .Where(i => selectedIds.Contains(i.Id))
                .ToListAsync();

            _context.InventoryItems.RemoveRange(itemsToDelete);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{itemsToDelete.Count} items deleted.";
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



        // GET: Edit an item in Tier3Dashboard
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var item = await _context.InventoryItems.FindAsync(id);
            return item == null ? NotFound() : View(item);
        }

        // POST: Update item details
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
                    return RedirectToAction(nameof(Tier3Dashboard));
                }
                catch
                {
                    return Problem("There was an error updating the item.");
                }
            }
            return View(inventoryItem);
        }

        // GET: Tier3/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Tier3/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Vendor,DeviceType,SerialNumber,HostName,AssetTag,PartID,FutureLocation,DateReceived,CurrentLocation,Status,BackOrdered,Notes,ProductDescription,Ready,LegacyDevice")] InventoryItem inventoryItem)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(inventoryItem);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Item created successfully!";
                    return RedirectToAction(nameof(Tier3Dashboard));
                }
                catch (Exception ex)
                {
                    // Optional: Log the exception
                    ModelState.AddModelError(string.Empty, "An error occurred while creating the item. Please try again.");
                }
            }
            return View(inventoryItem);
        }


    }
}
