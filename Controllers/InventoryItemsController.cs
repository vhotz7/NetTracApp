using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTracApp.Data;
using NetTracApp.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using NetTracApp.Services;

namespace NetTracApp.Controllers
{
    public class InventoryItemsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CsvService _csvService;

        // Constructor to inject the database context and CsvService
        public InventoryItemsController(ApplicationDbContext context, CsvService csvService)
        {
            _context = context;
            _csvService = csvService;
        }

        // GET: Display a list of inventory items with search functionality
        public async Task<IActionResult> Index(string searchString)
        {
            var items = from i in _context.InventoryItems select i;

            // Filter inventory items based on the search string
            if (!string.IsNullOrEmpty(searchString))
            {
                items = items.Where(s => s.Vendor.Contains(searchString) || s.SerialNumber.Contains(searchString));
            }

            // Return the filtered list to the view
            return View(await items.ToListAsync());
        }

        // GET: Show form to create a new inventory item
        public IActionResult Create()
        {
            return View();
        }

        // POST: Handle form submission for creating a new inventory item
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Vendor,DeviceType,SerialNumber,HostName,AssetTag,PartID,FutureLocation,DateReceived,CurrentLocation,Status,BackOrdered,Notes,ProductDescription,Ready,LegacyDevice,CreatedBy,ModifiedBy")] InventoryItem inventoryItem)
        {
            if (ModelState.IsValid)
            {
                _context.Add(inventoryItem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(inventoryItem);
        }

        // GET: Show form to edit an existing inventory item
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventoryItem = await _context.InventoryItems.FindAsync(id);
            if (inventoryItem == null)
            {
                return NotFound();
            }
            return View(inventoryItem);
        }

        // POST: Handle form submission for editing an existing inventory item
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Vendor,DeviceType,SerialNumber,HostName,AssetTag,PartID,FutureLocation,DateReceived,CurrentLocation,Status,BackOrdered,Notes,ProductDescription,Ready,LegacyDevice,CreatedBy,ModifiedBy")] InventoryItem inventoryItem)
        {
            if (id != inventoryItem.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(inventoryItem);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InventoryItemExists(inventoryItem.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(inventoryItem);
        }

        // GET: Confirm deletion of an inventory item
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventoryItem = await _context.InventoryItems
                .FirstOrDefaultAsync(m => m.Id == id);
            if (inventoryItem == null)
            {
                return NotFound();
            }

            return View(inventoryItem);
        }

        // POST: Handle deletion of an inventory item for Tier 3 approval
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var inventoryItem = await _context.InventoryItems.FindAsync(id);

            if (inventoryItem == null)
            {
                return NotFound();
            }

            // Check if the user is a Tier 3 user
            if (User.IsInRole("Tier3"))
            {
                // Allow Tier 3 to delete the item
                _context.InventoryItems.Remove(inventoryItem);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Item deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                // For non-Tier 3 users, mark the item as pending deletion
                inventoryItem.PendingDeletion = true;
                inventoryItem.DeletionApproved = false; // Ensure it's not yet approved

                _context.InventoryItems.Update(inventoryItem);
                await _context.SaveChangesAsync();

                // Inform the user that the item is awaiting approval
                TempData["InfoMessage"] = "Item deletion is waiting for Tier 3 approval.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Handle bulk upload of inventory items from CSV files
        [HttpPost]
        public async Task<IActionResult> UploadFile(List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
            {
                ModelState.AddModelError("files", "Please select one or more CSV files.");
                return RedirectToAction("Index");
            }

            var totalNewRecords = 0;

            foreach (var file in files)
            {
                if (!Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError("files", "Only CSV files are allowed.");
                    continue;
                }

                try
                {
                    var inventoryItems = new List<InventoryItem>();
                    using (var stream = file.OpenReadStream())
                    {
                        // Use CsvService to read and process the CSV file
                        inventoryItems = _csvService.ReadCsvFile(stream).ToList();
                    }

                    // Filter out duplicate records based on SerialNumber
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
                    ModelState.AddModelError("files", $"An error occurred while processing file '{file.FileName}': {ex.Message}");
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

            return RedirectToAction("Index");
        }

        // POST: Handle request deletion (non-Tier 3 users)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestDelete(int id)
        {
            var inventoryItem = await _context.InventoryItems.FindAsync(id);

            if (inventoryItem == null)
            {
                return NotFound();
            }

            // For non-Tier 3 users, mark the item as pending deletion
            inventoryItem.PendingDeletion = true;
            inventoryItem.DeletionApproved = false; // Ensure it's not yet approved

            _context.InventoryItems.Update(inventoryItem);
            await _context.SaveChangesAsync();

            // Inform the user that the item is awaiting approval
            TempData["InfoMessage"] = "Item deletion is waiting for Tier 3 approval.";
            return RedirectToAction(nameof(Index));
        }

        // Helper method to check if an inventory item exists
        private bool InventoryItemExists(int id)
        {
            return _context.InventoryItems.Any(e => e.Id == id);
        }
    }
}
