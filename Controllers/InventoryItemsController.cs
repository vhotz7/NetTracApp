using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTracApp.Data;
using NetTracApp.Models;
using CsvHelper;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace NetTracApp.Controllers
{
    public class InventoryItemsController : Controller
    {
        private readonly ApplicationDbContext _context;

        // constructor to inject the database context
        public InventoryItemsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // action to display a list of inventory items with search functionality
        public async Task<IActionResult> Index(string searchString)
        {
            // get all inventory items
            var items = from i in _context.InventoryItems select i;

            // filter inventory items based on the search string
            if (!string.IsNullOrEmpty(searchString))
            {
                items = items.Where(s => s.Vendor.Contains(searchString) || s.SerialNumber.Contains(searchString));
            }

            // return the filtered list to the view
            return View(await items.ToListAsync());
        }

        // action to display the create form
        public IActionResult Create()
        {
            return View();
        }

        // action to handle form submission for creating a new inventory item
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Vendor,DeviceType,SerialNumber,HostName,AssetTag,PartID,FutureLocation,DateReceived,CurrentLocation,Status,BackOrdered,Notes,ProductDescription,Ready,LegacyDevice,CreatedBy,ModifiedBy")] InventoryItem inventoryItem)
        {
            if (ModelState.IsValid)
            {
                // add the new inventory item to the database
                _context.Add(inventoryItem);
                await _context.SaveChangesAsync(); // save changes
                return RedirectToAction(nameof(Index)); // redirect to index
            }
            return View(inventoryItem);
        }

        // action to display the edit form for a specific inventory item
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound(); // return not found if id is null
            }

            var inventoryItem = await _context.InventoryItems.FindAsync(id);
            if (inventoryItem == null)
            {
                return NotFound(); // return not found if item doesn't exist
            }
            return View(inventoryItem); // return the edit view with the item
        }

        // action to handle form submission for editing an existing inventory item
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Vendor,DeviceType,SerialNumber,HostName,AssetTag,PartID,FutureLocation,DateReceived,CurrentLocation,Status,BackOrdered,Notes,ProductDescription,Ready,LegacyDevice,CreatedBy,ModifiedBy")] InventoryItem inventoryItem)
        {
            if (id != inventoryItem.Id)
            {
                return NotFound(); // return not found if the id does not match
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(inventoryItem); // update the inventory item
                    await _context.SaveChangesAsync(); // save changes
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InventoryItemExists(inventoryItem.Id))
                    {
                        return NotFound(); // handle concurrency exception if item doesn't exist
                    }
                    else
                    {
                        throw; // rethrow the exception if another issue occurs
                    }
                }
                return RedirectToAction(nameof(Index)); // redirect to index
            }
            return View(inventoryItem); // return the view with the item
        }

        // action to display the delete confirmation page
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound(); // return not found if id is null
            }

            var inventoryItem = await _context.InventoryItems
                .FirstOrDefaultAsync(m => m.Id == id);
            if (inventoryItem == null)
            {
                return NotFound(); // return not found if item doesn't exist
            }

            return View(inventoryItem); // return the delete confirmation view
        }

        // action to handle the deletion of an inventory item
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var inventoryItem = await _context.InventoryItems.FindAsync(id);
            if (inventoryItem != null)
            {
                _context.InventoryItems.Remove(inventoryItem); // remove the item from the database
                await _context.SaveChangesAsync(); // save changes
            }
            return RedirectToAction(nameof(Index)); // redirect to index
        }

        // action to handle bulk upload of inventory items from a CSV file
        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            // check if the file is null or empty
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("file", "Please select a valid CSV file.");
                return RedirectToAction(nameof(Index));
            }

            // check if the file has a .csv extension
            if (!Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("file", "Only CSV files are allowed.");
                return RedirectToAction(nameof(Index));
            }

            try
            {
                using (var stream = new StreamReader(file.OpenReadStream()))
                using (var csv = new CsvReader(stream, CultureInfo.InvariantCulture))
                {
                    // read the CSV file and convert it to a list of inventory items
                    var records = csv.GetRecords<InventoryItem>().ToList();
                    var newRecords = new List<InventoryItem>();

                    // filter out duplicate records
                    foreach (var record in records)
                    {
                        if (!_context.InventoryItems.Any(e => e.Id == record.Id))
                        {
                            newRecords.Add(record); // add only the new records
                        }
                    }

                    // add new records to the database
                    if (newRecords.Any())
                    {
                        _context.InventoryItems.AddRange(newRecords);
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = $"{newRecords.Count} new records added successfully.";
                    }
                    else
                    {
                        TempData["InfoMessage"] = "No new records to add.";
                    }
                }
            }
            catch (Exception ex)
            {
                // handle errors during file processing
                ModelState.AddModelError("file", $"An error occurred while processing the file: {ex.Message}");
            }

            return RedirectToAction(nameof(Index)); // redirect to index
        }

        // helper method to check if an inventory item exists
        private bool InventoryItemExists(int id)
        {
            return _context.InventoryItems.Any(e => e.Id == id);
        }
    }
}
