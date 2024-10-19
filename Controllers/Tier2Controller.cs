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

        // Inject ApplicationDbContext and CsvService into the controller
        public Tier2Controller(ApplicationDbContext context, CsvService csvService)
        {
            _context = context;
            _csvService = csvService;
        }

        // GET: Tier2/Tier2Dashboard
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
        




        // POST: Handle file uploads
        [HttpPost]
        public async Task<IActionResult> UploadFile(List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
            {
                ModelState.AddModelError("files", "Please select one or more CSV files.");
                return RedirectToAction(nameof(Tier2Dashboard));
            }

            var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

            var totalNewRecords = 0;
            var duplicateRecords = new List<string>();

            foreach (var file in files)
            {
                if (!Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError("files", "Only CSV files are allowed.");
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
                catch (Exception ex)
                {
                    ModelState.AddModelError("files", $"Error processing file '{file.FileName}': {ex.Message}");
                }
            }

            TempData["SuccessMessage"] = $"{totalNewRecords} new items uploaded successfully.";
            TempData["DuplicateMessage"] = duplicateRecords.Any() ? $"Duplicates found: {string.Join(", ", duplicateRecords)}" : "No duplicates found.";
            return RedirectToAction(nameof(Tier2Dashboard));
        }
<<<<<<< HEAD
=======
        // GET: /Tier2/Tier2Dashboard


>>>>>>> a7f4f09f79e2a94d4d73ee04c92dca413a813b8e

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
                TempData["ErrorMessage"] = $"Error generating the new file: {ex.Message}";
                return RedirectToAction(nameof(Tier2Dashboard));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var inventoryItem = await _context.InventoryItems.FindAsync(id);
            if (inventoryItem == null) return NotFound();

            if (User.IsInRole("Tier3"))
            {
                _context.InventoryItems.Remove(inventoryItem);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Item deleted successfully.";
            }
            else
            {
                inventoryItem.PendingDeletion = true;
                inventoryItem.DeletionApproved = false;
                _context.InventoryItems.Update(inventoryItem);
                await _context.SaveChangesAsync();
                TempData["InfoMessage"] = "Item marked for deletion and awaiting Tier 3 approval.";
            }

            return RedirectToAction(nameof(Tier2Dashboard));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSelected(List<int> selectedIds)
        {
            if (selectedIds == null || !selectedIds.Any())
            {
                TempData["InfoMessage"] = "No items selected for deletion.";
                return RedirectToAction("Tier2Dashboard", "Tier2");
            }

            var itemsToUpdate = await _context.InventoryItems
                .Where(i => selectedIds.Contains(i.Id))
                .ToListAsync();

            foreach (var item in itemsToUpdate)
            {
                item.PendingDeletion = true;
                item.DeletionApproved = false;
            }

            _context.InventoryItems.UpdateRange(itemsToUpdate);
            await _context.SaveChangesAsync();

            TempData["InfoMessage"] = "Items marked for deletion, awaiting Tier 3 approval.";
            return RedirectToAction("Tier2Dashboard", "Tier2");
        }





        private bool InventoryItemExists(int id) => _context.InventoryItems.Any(e => e.Id == id);

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.InventoryItems.FindAsync(id);
            return item == null ? NotFound() : View(item);
        }

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

        public IActionResult Create() => View();

        [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> RequestDelete([FromForm] List<int> selectedItems)
{
    if (selectedItems == null || !selectedItems.Any())
    {
        TempData["InfoMessage"] = "No items selected for deletion.";
        return RedirectToAction(nameof(Tier2Dashboard));
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
<<<<<<< HEAD

        _context.InventoryItems.UpdateRange(itemsToUpdate);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Selected items marked for deletion and sent to Tier 3 for approval.";
    }
    catch (Exception ex)
    {
        TempData["ErrorMessage"] = $"Error: {ex.Message}";
    }

    return RedirectToAction(nameof(Tier2Dashboard));
}

=======
        // GET: Tier2/Edit/{id}
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var inventoryItem = await _context.InventoryItems.FindAsync(id);
            if (inventoryItem == null) return NotFound();

            return View(inventoryItem);
        }

        // POST: Tier2/Edit/{id}
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
                    return RedirectToAction(nameof(Tier2Dashboard)); // Redirect to Tier 2 dashboard
                }
                catch (Exception)
                {
                    return Problem("There was an error updating the item.");
                }
            }

            return View(inventoryItem); // Reload the view if invalid data is found
        }
        // GET: Tier2/Index (or just /Tier2)
        public async Task<IActionResult> Index()
        {
            var items = await _context.InventoryItems.ToListAsync();
            return View(items); // Ensure you have an Index view
        }

        // GET: Tier2/Create
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
                return RedirectToAction(nameof(Tier2Dashboard));
            }
            return View(inventoryItem);
        }
    }
}