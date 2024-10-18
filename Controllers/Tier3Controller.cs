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

        public Tier3Controller(ApplicationDbContext context, CsvService csvService)
        {
            _context = context;
            _csvService = csvService;
        }
      
        // GET: Show form to create a new inventory item
        public IActionResult Create()
        {
            return View();
        }

        // POST: Handle form submission for creating a new inventory item
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Vendor,DeviceType,SerialNumber,HostName,AssetTag,PartID,FutureLocation,DateReceived,CurrentLocation,Status,BackOrdered,Notes,ProductDescription,Ready,LegacyDevice")] InventoryItem inventoryItem)
        {
            if (ModelState.IsValid)
            {
                _context.Add(inventoryItem);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Item created successfully!";
                return RedirectToAction(nameof(ApproveDeletions));
            }
            // If the model is invalid, stay on the same page
            return View(inventoryItem);
        }


            // Displays items awaiting deletion approval and current inventory with search functionality
            public async Task<IActionResult> ApproveDeletions(string? searchString)
        {
            var inventoryItems = _context.InventoryItems.AsQueryable();

            // Filter based on the search string if provided
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                inventoryItems = inventoryItems.Where(i =>
                    (i.Vendor ?? string.Empty).Contains(searchString) ||
                    (i.SerialNumber ?? string.Empty).Contains(searchString));
            }

            var itemList = await inventoryItems.ToListAsync();
            return View(itemList);
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSelected(List<int> selectedItems)
        {
            if (selectedItems == null || !selectedItems.Any())
            {
                return BadRequest("No items selected for deletion.");
            }

            // Retrieve items from the database
            var itemsToDelete = await _context.InventoryItems
                .Where(i => selectedItems.Contains(i.Id)).ToListAsync();

            if (User.IsInRole("Tier3")) // Check if the user is Tier 3
            {
                // Tier 3: Directly delete the items
                _context.InventoryItems.RemoveRange(itemsToDelete);
                await _context.SaveChangesAsync();

                return Ok($"{itemsToDelete.Count} items deleted successfully.");
            }
            else
            {
                // Tier 2: Mark items for deletion, pending approval
                foreach (var item in itemsToDelete)
                {
                    item.PendingDeletion = true;
                    item.DeletionApproved = false;
                }

                await _context.SaveChangesAsync();
                return Ok("Items marked for deletion and pending approval.");
            }
        }
        // GET: Tier3/Edit/{id}
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var inventoryItem = await _context.InventoryItems.FindAsync(id);
            if (inventoryItem == null) return NotFound();

            return View(inventoryItem);
        }

        // POST: Tier3/Edit/{id}
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
                    return RedirectToAction(nameof(ApproveDeletions));
                }
                catch (Exception)
                {
                    return Problem("There was an error updating the item.");
                }
            }

            return View(inventoryItem); // Reload the view if invalid data is found
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
        [HttpPost("/Inventory/DeleteItems")]
        public async Task<IActionResult> DeleteItems([FromBody] List<int> itemIds)
        {
            // Check if the current user is a Tier 3 user
            if (User.IsInRole("Tier3")) // Adjust to your role management logic
            {
                var itemsToDelete = await _context.InventoryItems
                    .Where(item => itemIds.Contains(item.Id))
                    .ToListAsync();

                if (itemsToDelete.Count == 0)
                {
                    return NotFound(new { message = "No items found to delete." });
                }

                // Delete items directly for T3 users
                _context.InventoryItems.RemoveRange(itemsToDelete);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Items deleted successfully." });
            }
            else
            {
                // Route deletion requests for Tier 2 users to approval
                var itemsToUpdate = await _context.InventoryItems
                    .Where(item => itemIds.Contains(item.Id))
                    .ToListAsync();

                if (itemsToUpdate.Count == 0)
                {
                    return NotFound(new { message = "No items found to request deletion." });
                }

                foreach (var item in itemsToUpdate)
                {
                    item.PendingDeletion = true;
                    item.DeletionApproved = false;
                }

                _context.InventoryItems.UpdateRange(itemsToUpdate);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Deletion request submitted for approval." });
            }
          
            }


        }

    }
