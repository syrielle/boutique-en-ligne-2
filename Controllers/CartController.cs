using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ECommerceBoutique.Data;
using ECommerceBoutique.Models;
using Microsoft.AspNetCore.Identity;

namespace ECommerceBoutique.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public CartController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Cart
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return View(cart);
        }

        // POST: Cart/AddToCart/5
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
                cartItem.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                cartItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = productId,
                    Quantity = quantity,
                    CreatedAt = DateTime.UtcNow
                };
                cart.Items.Add(cartItem);
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: Cart/UpdateQuantity/5
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            var user = await _userManager.GetUserAsync(User);
            var cartItem = await _context.CartItems
                .Include(i => i.Cart)
                .FirstOrDefaultAsync(i => i.Id == cartItemId && i.Cart.UserId == user.Id);

            if (cartItem == null)
            {
                return NotFound();
            }

            if (quantity <= 0)
            {
                _context.CartItems.Remove(cartItem);
            }
            else
            {
                cartItem.Quantity = quantity;
                cartItem.UpdatedAt = DateTime.UtcNow;
            }

            cartItem.Cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: Cart/RemoveItem/5
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            var user = await _userManager.GetUserAsync(User);
            var cartItem = await _context.CartItems
                .Include(i => i.Cart)
                .FirstOrDefaultAsync(i => i.Id == cartItemId && i.Cart.UserId == user.Id);

            if (cartItem == null)
            {
                return NotFound();
            }

            _context.CartItems.Remove(cartItem);
            cartItem.Cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
} 