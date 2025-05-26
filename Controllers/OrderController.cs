using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ECommerceBoutique.Data;
using ECommerceBoutique.Models;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace ECommerceBoutique.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            ApplicationDbContext context,
            UserManager<User> userManager,
            ILogger<OrderController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Order
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        // GET: Order/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Order/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (cart == null || !cart.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            decimal total = cart.Items.Sum(item => item.Product.Price * item.Quantity);

            var order = new Order
            {
                UserId = user.Id,
                User = user,
                ShippingAddress = user.Address ?? "",
                ShippingCity = user.City ?? "",
                ShippingPostalCode = user.PostalCode ?? "",
                ShippingCountry = user.Country ?? "",
                Total = total,
                Status = "En attente de paiement",
                CreatedAt = DateTime.UtcNow,
                OrderItems = new List<OrderItem>()
            };

            foreach (var cartItem in cart.Items)
            {
                order.OrderItems.Add(new OrderItem
                {
                    ProductId = cartItem.ProductId,
                    Product = cartItem.Product,
                    Quantity = cartItem.Quantity,
                    UnitPrice = cartItem.Product.Price
                });
            }

            return View(order);
        }

        // POST: Order/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order order)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogError("Utilisateur non trouvé");
                    ModelState.AddModelError("", "Une erreur s'est produite : utilisateur non trouvé.");
                    return View(order);
                }

                var cart = await _context.Carts
                    .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (cart == null || !cart.Items.Any())
                {
                    _logger.LogError("Panier non trouvé ou vide");
                    return RedirectToAction("Index", "Cart");
                }

                // Vérifier les informations de livraison
                if (string.IsNullOrEmpty(order.ShippingAddress) ||
                    string.IsNullOrEmpty(order.ShippingCity) ||
                    string.IsNullOrEmpty(order.ShippingPostalCode) ||
                    string.IsNullOrEmpty(order.ShippingCountry))
                {
                    _logger.LogWarning("Informations de livraison manquantes");
                    ModelState.AddModelError("", "Veuillez remplir toutes les informations de livraison.");
                    
                    // Recharger les données du panier
                    var cartItems = cart.Items.Select(item => new OrderItem
                    {
                        ProductId = item.ProductId,
                        Product = item.Product,
                        Quantity = item.Quantity,
                        UnitPrice = item.Product.Price
                    }).ToList();

                    order.OrderItems = cartItems;
                    order.Total = cartItems.Sum(item => item.UnitPrice * item.Quantity);
                    return View(order);
                }

                // Créer la commande
                order.UserId = user.Id;
                order.User = user;
                order.Total = cart.Items.Sum(item => item.Product.Price * item.Quantity);
                order.Status = "En attente de confirmation";
                order.CreatedAt = DateTime.UtcNow;

                // Créer les éléments de la commande sans référence à Product
                order.OrderItems = new List<OrderItem>();
                foreach (var item in cart.Items)
                {
                    order.OrderItems.Add(new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Product.Price
                    });
                }

                _context.Orders.Add(order);

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, "Erreur de base de données lors de la création de la commande");
                    var innerMessage = dbEx.InnerException?.Message ?? "Pas de détails supplémentaires";
                    ModelState.AddModelError("", $"Erreur de base de données : {innerMessage}");
                    return View(order);
                }

                // Vider le panier seulement si la commande a été créée avec succès
                _context.CartItems.RemoveRange(cart.Items);
                _context.Carts.Remove(cart);
                await _context.SaveChangesAsync();

                return RedirectToAction("Process", "Payment", new { id = order.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la commande");
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                ModelState.AddModelError("", $"Une erreur s'est produite lors de la création de la commande. Détails : {errorMessage}");
                
                // Recharger les données du panier pour la vue
                var user = await _userManager.GetUserAsync(User);
                var cart = await _context.Carts
                    .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (cart != null && cart.Items.Any())
                {
                    var cartItems = cart.Items.Select(item => new OrderItem
                    {
                        ProductId = item.ProductId,
                        Product = item.Product,
                        Quantity = item.Quantity,
                        UnitPrice = item.Product.Price
                    }).ToList();

                    order.OrderItems = cartItems;
                    order.Total = cartItems.Sum(item => item.UnitPrice * item.Quantity);
                }

                return View(order);
            }
        }
    }
} 