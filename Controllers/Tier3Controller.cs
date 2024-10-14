using Microsoft.AspNetCore.Mvc;
using NetTracApp.Data;
using System.Linq;
using System.Threading.Tasks;

namespace NetTracApp.Controllers
{
    public class Tier3Controller : Controller
    {
        private readonly ApplicationDbContext _context;

        public Tier3Controller(ApplicationDbContext context)
        {
            _context = context;
        }

        // Displays items awaiting deletion approval and current inventory
        public IActionResult ApproveDeletions()
        {
            // Fetch both pending items and all inventory items
            var inventoryItems = _context.InventoryItems.ToList();

            // Pass all inventory items to the view (including those pending deletion)
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

            // Approve and delete the item
            _context.InventoryItems.Remove(inventoryItem);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Item deletion approved and item removed from inventory.";

            // After approving the deletion, redirect the user back to the ApproveDeletions page
            return RedirectToAction(nameof(ApproveDeletions));
        }

        // Handles direct deletion of items (for users like Tier 3)
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var inventoryItem = await _context.InventoryItems.FindAsync(id);

            if (inventoryItem == null)
            {
                return NotFound();
            }

            // Delete the item directly
            _context.InventoryItems.Remove(inventoryItem);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Item deleted successfully.";

            // Redirect the user back to the ApproveDeletions page after deletion
            return RedirectToAction(nameof(ApproveDeletions));
        }
    }
}
