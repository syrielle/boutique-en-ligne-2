using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ECommerceBoutique.Data;
using ECommerceBoutique.Models;
using Microsoft.AspNetCore.Identity;

namespace ECommerceBoutique.Controllers
{
    [Authorize(Roles = "Vendor")]
    public class VendorOrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public VendorOrderController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: VendorOrder
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .Where(o => o.OrderItems.Any(oi => oi.Product.VendorId == currentUser.Id))
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        // GET: VendorOrder/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id && 
                    o.OrderItems.Any(oi => oi.Product.VendorId == currentUser.Id));

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: VendorOrder/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id && 
                    o.OrderItems.Any(oi => oi.Product.VendorId == currentUser.Id));

            if (order == null)
            {
                return NotFound();
            }

            order.Status = status;
            order.UpdatedAt = DateTime.UtcNow;

            if (status == "Expédiée")
            {
                order.ShippedAt = DateTime.UtcNow;
            }
            else if (status == "Livrée")
            {
                order.DeliveredAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }
    }
} 