using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ECommerceBoutique.Data;
using ECommerceBoutique.Models;
using ECommerceBoutique.Services;
using Microsoft.AspNetCore.Identity;

namespace ECommerceBoutique.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly FakeStoreService _fakeStoreService;
        private readonly UserManager<User> _userManager;

        public ProductController(
            ApplicationDbContext context, 
            FakeStoreService fakeStoreService,
            UserManager<User> userManager)
        {
            _context = context;
            _fakeStoreService = fakeStoreService;
            _userManager = userManager;
        }

        // GET: Product
        public async Task<IActionResult> Index(string searchString, string category)
        {
            var products = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => 
                    p.Name.Contains(searchString) || 
                    p.Description.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(category))
            {
                products = products.Where(p => p.Category == category);
            }

            ViewBag.Categories = await _context.Products
                .Select(p => p.Category)
                .Distinct()
                .ToListAsync();
                
            // Passer les valeurs actuelles des filtres à la vue
            ViewBag.CurrentSearchString = searchString;
            ViewBag.CurrentCategory = category;

            return View(await products.ToListAsync());
        }

        // GET: Product/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Product/Create
        [Authorize(Roles = "Vendor")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> Create([Bind("Name,Description,Price,ImageUrl,Category,StockQuantity")] Product product)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                product.VendorId = currentUser.Id;
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: Product/ImportFromFakeStore
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> ImportFromFakeStore()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var products = await _fakeStoreService.GetProductsAsync();
            
            foreach (var product in products)
            {
                if (!await _context.Products.AnyAsync(p => p.Name == product.Name))
                {
                    product.VendorId = currentUser.Id;
                    _context.Products.Add(product);
                }
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Product/Edit/5
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            
            // Vérifier que le vendeur est propriétaire du produit
            var currentUser = await _userManager.GetUserAsync(User);
            if (product.VendorId != currentUser.Id)
            {
                return Forbid();
            }
            
            return View(product);
        }

        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,ImageUrl,Category,StockQuantity")] Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            var existingProduct = await _context.Products.FindAsync(id);
            if (existingProduct == null)
            {
                return NotFound();
            }

            // Vérifier que le vendeur est propriétaire du produit
            var currentUser = await _userManager.GetUserAsync(User);
            if (existingProduct.VendorId != currentUser.Id)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Mettre à jour les propriétés individuellement
                    existingProduct.Name = product.Name;
                    existingProduct.Description = product.Description;
                    existingProduct.Price = product.Price;
                    existingProduct.ImageUrl = product.ImageUrl;
                    existingProduct.Category = product.Category;
                    existingProduct.StockQuantity = product.StockQuantity;
                    existingProduct.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
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
            return View(product);
        }

        // GET: Product/Delete/5
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            // Vérifier que le vendeur est propriétaire du produit
            var currentUser = await _userManager.GetUserAsync(User);
            if (product.VendorId != currentUser.Id)
            {
                return Forbid();
            }

            return View(product);
        }

        // POST: Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Vérifier que le vendeur est propriétaire du produit
            var currentUser = await _userManager.GetUserAsync(User);
            if (product.VendorId != currentUser.Id)
            {
                return Forbid();
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
} 