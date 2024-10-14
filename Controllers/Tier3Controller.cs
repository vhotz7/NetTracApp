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

        // Displays items awaiting deletion approval
        public IActionResult ApproveDeletions()
        {
            var pendingItems = _context.InventoryItems.Where(i => i.PendingDeletion && !i.DeletionApproved).ToList();
            return View(pendingItems);
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

            // Approve deletion: remove the item from inventory
            _context.InventoryItems.Remove(inventoryItem);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Item deletion approved and item removed from inventory.";
            return RedirectToAction(nameof(ApproveDeletions));
        }
    }
}
