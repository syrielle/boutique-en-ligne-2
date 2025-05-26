using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ECommerceBoutique.Data;
using ECommerceBoutique.Models;
using ECommerceBoutique.Services;
using Microsoft.AspNetCore.Identity;

namespace ECommerceBoutique.Controllers
{
    [Authorize]
    public class InvoiceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly InvoiceService _invoiceService;

        public InvoiceController(
            ApplicationDbContext context,
            UserManager<User> userManager,
            InvoiceService invoiceService)
        {
            _context = context;
            _userManager = userManager;
            _invoiceService = invoiceService;
        }

        // GET: Invoice (pour les clients)
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            try
            {
                // Récupérer toutes les commandes payées de l'utilisateur
                var userOrders = await _context.Orders
                    .Where(o => o.UserId == user.Id && o.Status == "Payée")
                    .Select(o => o.Id)
                    .ToListAsync();

                // Récupérer les factures correspondant à ces commandes
                var invoices = await _context.Invoices
                    .Include(i => i.Items)
                    .Include(i => i.Order)
                    .Include(i => i.Vendor)
                    .Where(i => userOrders.Contains(i.OrderId))
                    .OrderByDescending(i => i.InvoiceDate)
                    .ToListAsync();

                // Calculer le total des achats
                var totalSpent = invoices
                    .Where(i => i.PaymentStatus == "Payée")
                    .Sum(i => i.TotalAmount);

            ViewBag.TotalSpent = totalSpent;

            return View(invoices);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la récupération des factures: {ex.Message}");
                ViewBag.TotalSpent = 0;
                return View(new List<Invoice>());
            }
        }

        // GET: Invoice/VendorInvoices (pour les vendeurs)
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> VendorInvoices()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            try
            {
                // Récupérer toutes les factures du vendeur
                var invoices = await _context.Invoices
                    .Include(i => i.Items)
                    .Include(i => i.Order)
                    .Include(i => i.Order.User)  // Pour avoir les informations de l'acheteur
                    .Where(i => i.VendorId == user.Id)
                    .OrderByDescending(i => i.InvoiceDate)
                    .ToListAsync();

                // Calculer le total des gains
                var totalEarnings = invoices
                    .Where(i => i.PaymentStatus == "Payée")
                    .Sum(i => i.VendorTotal);

            ViewBag.TotalEarnings = totalEarnings;

            return View(invoices);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la récupération des factures vendeur: {ex.Message}");
                ViewBag.TotalEarnings = 0;
                return View(new List<Invoice>());
            }
        }

        // GET: Invoice/Details/5 (pour les clients)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var invoice = await _context.Invoices
                .Include(i => i.Items)
                .Include(i => i.Buyer)
                .Include(i => i.Vendor)
                .Include(i => i.Order)
                .FirstOrDefaultAsync(i => i.Id == id && i.BuyerId == user.Id);

            if (invoice == null)
            {
                return NotFound();
            }

            return View(invoice);
        }

        // GET: Invoice/VendorDetails/5 (pour les vendeurs)
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> VendorDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var invoice = await _context.Invoices
                .Include(i => i.Items)
                .Include(i => i.Buyer)
                .Include(i => i.Vendor)
                .Include(i => i.Order)
                .FirstOrDefaultAsync(i => i.Id == id && i.VendorId == user.Id);

            if (invoice == null)
            {
                return NotFound();
            }

            return View(invoice);
        }

        // Action temporaire pour régénérer les factures manquantes
        [Authorize]
        public async Task<IActionResult> RegenerateInvoices()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Challenge();
                }

                // Récupérer toutes les commandes payées
                var paidOrders = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .Include(o => o.User)
                    .Where(o => o.Status == "Payée")
                    .ToListAsync();

                int invoicesCreated = 0;
                var errors = new List<string>();

                foreach (var order in paidOrders)
                {
                    try
                    {
                        var vendorIds = order.OrderItems
                            .Where(oi => oi.Product != null && !string.IsNullOrEmpty(oi.Product.VendorId))
                            .Select(oi => oi.Product.VendorId)
                            .Distinct()
                            .ToList();

                        foreach (var vendorId in vendorIds)
                        {
                            // Vérifier si une facture existe déjà
                            var existingInvoice = await _context.Invoices
                                .FirstOrDefaultAsync(i => i.OrderId == order.Id && i.VendorId == vendorId);

                            if (existingInvoice == null)
                            {
                                using (var transaction = await _context.Database.BeginTransactionAsync())
                                {
                                    try
                                    {
                                        var invoice = await _invoiceService.GenerateInvoiceAsync(order, vendorId);
                                        if (invoice != null)
                                        {
                                            invoice.PaymentStatus = "Payée";
                                            invoice.PaidAt = order.PaidAt;
                                            await _context.SaveChangesAsync();
                                            await transaction.CommitAsync();
                                            invoicesCreated++;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        await transaction.RollbackAsync();
                                        errors.Add($"Erreur pour la commande {order.Id}, vendeur {vendorId}: {ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Erreur pour la commande {order.Id}: {ex.Message}");
                    }
                }

                if (errors.Any())
                {
                    TempData["Error"] = $"Certaines factures n'ont pas pu être générées. {string.Join(" | ", errors)}";
                }
                else if (invoicesCreated > 0)
                {
                    TempData["Message"] = $"{invoicesCreated} factures ont été régénérées avec succès.";
                }
                else
                {
                    TempData["Message"] = "Aucune nouvelle facture à générer.";
                }

                // Rediriger vers la bonne vue en fonction du rôle de l'utilisateur
                if (await _userManager.IsInRoleAsync(user, "Vendor"))
                {
                    return RedirectToAction(nameof(VendorInvoices));
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Erreur lors de la régénération des factures : {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
} 