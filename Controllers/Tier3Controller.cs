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
            _csvService = csvService;
        }

        // Displays items awaiting deletion approval and current inventory with search functionality
        public async Task<IActionResult> ApproveDeletions(string? searchString)
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

        // GET: Tier3 Dashboard with search functionality
        public async Task<IActionResult> Tier3Dashboard(string? searchString)
        {
            var items = _context.InventoryItems.AsQueryable();
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                items = items.Where(i =>
                    (i.Vendor ?? string.Empty).Contains(searchString) ||
                    (i.SerialNumber ?? string.Empty).Contains(searchString));
            }

            var itemList = await items.ToListAsync();
            return View(itemList);
        }

        // POST: Direct deletion for Tier 3 users
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

        // POST: Bulk delete or mark items for deletion approval
        [HttpPost("/Inventory/DeleteItems")]
        public async Task<IActionResult> DeleteItems([FromBody] List<int> itemIds)
        {
            var items = await _context.InventoryItems
                .Where(item => itemIds.Contains(item.Id))
                .ToListAsync();

            if (!items.Any()) return NotFound(new { message = "No items found." });

            if (User.IsInRole("Tier3"))
            {
                _context.InventoryItems.RemoveRange(items);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Items deleted successfully." });
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
                return Ok(new { message = "Deletion request submitted for approval." });
            }
        }

        // GET: Pending deletions
        public async Task<IActionResult> PendingDeletions()
        {
            var pendingDeletions = await _context.InventoryItems
                .Where(i => i.PendingDeletion && !i.DeletionApproved)
                .ToListAsync();
            return View(pendingDeletions);
        }

        // POST: Approve deletion request
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

        // POST: Deny deletion request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DenyDeletion(int id)
        {
            var item = await _context.InventoryItems.FindAsync(id);
            if (item == null) return NotFound();

            item.PendingDeletion = false;
            _context.InventoryItems.Update(item);
            await _context.SaveChangesAsync();

            TempData["InfoMessage"] = "Item deletion denied.";
            return RedirectToAction(nameof(PendingDeletions));
        }

        // GET: Edit item
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.InventoryItems.FindAsync(id);
            return item == null ? NotFound() : View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAll()
        {
            var items = _context.InventoryItems.ToList();

            if (items.Any())
            {
                // Directly delete all items without approval for Tier 3 users
                _context.InventoryItems.RemoveRange(items);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "All inventory items deleted successfully.";
            }
            else
            {
                TempData["InfoMessage"] = "No items available to delete.";
            }

            return RedirectToAction(nameof(ApproveDeletions));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFile(List<IFormFile> files)
        {
            if (files == null || !files.Any())
            {
                TempData["ErrorMessage"] = "Please select at least one CSV file.";
                return RedirectToAction(nameof(Tier3Dashboard));
            }

            var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

            int totalNewRecords = 0;
            var duplicateRecords = new List<string>();

        [HttpPost]
        public async Task<IActionResult> DeleteSelected(List<int> selectedIds)
        {
            if (selectedIds == null || !selectedIds.Any())
            {
                TempData["InfoMessage"] = "No items selected for deletion.";
                return RedirectToAction(nameof(ApproveDeletions));
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
                        // Directly delete items if the user is Tier 3
                        _context.InventoryItems.RemoveRange(itemsToDelete);
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = $"{itemsToDelete.Count} items deleted successfully.";
                    }
                    else
                    {
                        // Mark items as pending deletion for Tier 3 approval if user is Tier 2
                        foreach (var item in itemsToDelete)
                        {
                            item.PendingDeletion = true;
                            item.DeletionApproved = false; // Ensure it's marked as pending approval
                        }

                        _context.InventoryItems.UpdateRange(itemsToDelete);
                        await _context.SaveChangesAsync();
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

            return RedirectToAction(nameof(ApproveDeletions));
        }
        // Handles denying of deletions
        [HttpPost]
        public async Task<IActionResult> DenyDeletion(int id)
        {
            var inventoryItem = await _context.InventoryItems.FindAsync(id);

            TempData["SuccessMessage"] = $"{totalNewRecords} new items uploaded successfully.";
            if (duplicateRecords.Any())
            {
                TempData["DuplicateMessage"] = $"Duplicate items: {string.Join(", ", duplicateRecords)}";
            }

            return RedirectToAction(nameof(Tier3Dashboard));
        }


    }

}

