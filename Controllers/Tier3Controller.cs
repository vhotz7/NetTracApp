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
      
        // GET: Show form to create a new inventory item
        public IActionResult Create()
        {
            return View();
        }

<<<<<<< HEAD
        // POST: Upload CSV File
=======
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
>>>>>>> a7f4f09f79e2a94d4d73ee04c92dca413a813b8e
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

        // GET: Tier3Dashboard
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

<<<<<<< HEAD
       
    



// GET: Pending Deletions Page
public async Task<IActionResult> PendingDeletions()
        {
            var pendingDeletions = await _context.InventoryItems
                .Where(i => i.PendingDeletion && !i.DeletionApproved)
                .ToListAsync();

            return View(pendingDeletions);
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
=======
>>>>>>> a7f4f09f79e2a94d4d73ee04c92dca413a813b8e

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
<<<<<<< HEAD
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
=======
        public async Task<IActionResult> DeleteSelected(List<int> selectedItems)
>>>>>>> a7f4f09f79e2a94d4d73ee04c92dca413a813b8e
        {
            if (selectedItems == null || !selectedItems.Any())
            {
<<<<<<< HEAD
                TempData["InfoMessage"] = "No items selected.";
                return RedirectToAction(nameof(Tier3Dashboard));
            }

            var itemsToDelete = await _context.InventoryItems
                .Where(i => selectedIds.Contains(i.Id))
                .ToListAsync();

            _context.InventoryItems.RemoveRange(itemsToDelete);
=======
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
>>>>>>> a7f4f09f79e2a94d4d73ee04c92dca413a813b8e
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{itemsToDelete.Count} items deleted.";
            return RedirectToAction(nameof(Tier3Dashboard));
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

        // GET: Create a new item
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
<<<<<<< HEAD
}
=======
>>>>>>> a7f4f09f79e2a94d4d73ee04c92dca413a813b8e
