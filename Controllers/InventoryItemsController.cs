using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTracApp.Data;
using NetTracApp.Models;
using Microsoft.AspNetCore.Http;
using CsvHelper;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace NetTracApp.Controllers
{
    namespace NetTracApp.Controllers
    {
        public class InventoryItemsController : Controller
        {
            private readonly ApplicationDbContext _context;

            public InventoryItemsController(ApplicationDbContext context)
            {
                _context = context;
            }

            // GET: InventoryItems
            public async Task<IActionResult> Index(string searchString)
            {
                var items = from i in _context.InventoryItems select i;

                if (!string.IsNullOrEmpty(searchString))
                {
                    items = items.Where(s => s.Vendor.Contains(searchString) || s.SerialNumber.Contains(searchString));
                }

                return View(await items.ToListAsync());
            }

            // GET: InventoryItems/Create
            public IActionResult Create()
            {
                return View();
            }

            // POST: InventoryItems/Create
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

            // GET: InventoryItems/Edit/5
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

            // POST: InventoryItems/Edit/5
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

            // GET: InventoryItems/Delete/5
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

            // POST: InventoryItems/Delete/5
            [HttpPost, ActionName("Delete")]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> DeleteConfirmed(int id)
            {
                var inventoryItem = await _context.InventoryItems.FindAsync(id);
                _context.InventoryItems.Remove(inventoryItem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Bulk Upload from CSV
            [HttpPost]
            public async Task<IActionResult> UploadCsv(IFormFile file)
            {
                if (file != null && file.Length > 0)
                {
                    using (var stream = new StreamReader(file.OpenReadStream()))
                    using (var csv = new CsvReader(stream, CultureInfo.InvariantCulture))
                    {
                        var records = csv.GetRecords<InventoryItem>().ToList();
                        _context.InventoryItems.AddRange(records);
                        await _context.SaveChangesAsync();
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            private bool InventoryItemExists(int id)
            {
                return _context.InventoryItems.Any(e => e.Id == id);
            }
        }
    }
}
