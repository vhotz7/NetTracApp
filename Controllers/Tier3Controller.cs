using Microsoft.AspNetCore.Mvc;
using NetTracApp.Data;
using NetTracApp.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.IO;
using NetTracApp.Services;
using Microsoft.EntityFrameworkCore;

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
            var duplicateRecords = new List<string>();

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
                        inventoryItems = _csvService.ReadCsvFile(stream).ToList();
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

            return RedirectToAction(nameof(ApproveDeletions));
        }

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
                return RedirectToAction(nameof(ApproveDeletions));
            }
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
        public async Task<IActionResult> ApproveAll()
        {
            var pendingItems = _context.InventoryItems.Where(i => i.PendingDeletion && !i.DeletionApproved).ToList();

            if (pendingItems.Any())
            {
                _context.InventoryItems.RemoveRange(pendingItems);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "All pending deletions approved and removed from inventory.";
            }
            else
            {
                TempData["InfoMessage"] = "No items available for approval.";
            }

            return RedirectToAction(nameof(ApproveDeletions));
        }


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

            if (inventoryItem == null)
            {
                return NotFound();
            }

            // Set PendingDeletion to false and DeletionApproved to true (or however you want to handle it)
            inventoryItem.PendingDeletion = false;  // Remove from pending deletion
            inventoryItem.DeletionApproved = false;  // Reset or handle approval logic as necessary

            // Update the inventory item in the database
            _context.InventoryItems.Update(inventoryItem);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Item has been moved back to inventory and removed from pending status.";
            return RedirectToAction(nameof(ApproveDeletions));
        }


    }

}

