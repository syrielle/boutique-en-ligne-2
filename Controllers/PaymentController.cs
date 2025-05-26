using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ECommerceBoutique.Data;
using ECommerceBoutique.Models;
using ECommerceBoutique.Services;
using Microsoft.AspNetCore.Identity;
using Stripe;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ECommerceBoutique.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly StripeService _stripeService;
        private readonly IConfiguration _configuration;
        private readonly ECommerceBoutique.Services.InvoiceService _invoiceService;

        public PaymentController(
            ApplicationDbContext context,
            UserManager<User> userManager,
            StripeService stripeService,
            IConfiguration configuration,
            ECommerceBoutique.Services.InvoiceService invoiceService)
        {
            _context = context;
            _userManager = userManager;
            _stripeService = stripeService;
            _configuration = configuration;
            _invoiceService = invoiceService;
        }

        // GET: Payment/Process/5
        public async Task<IActionResult> Process(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);

            if (order == null)
            {
                return NotFound();
            }

            // Vérifier si la commande est en attente de paiement
            if (order.Status != "En attente de confirmation")
            {
                return RedirectToAction("Details", "Order", new { id = order.Id });
            }

            var clientSecret = await _stripeService.CreatePaymentIntent(order);

            ViewBag.StripePublishableKey = _configuration["Stripe:PublishableKey"];
            ViewBag.ClientSecret = clientSecret;
            ViewBag.OrderId = order.Id;

            return View(order);
        }

        // POST: Payment/Webhook
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Webhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _configuration["Stripe:WebhookSecret"]
                );

                if (stripeEvent.Type == "payment_intent.succeeded")
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    var orderId = int.Parse(paymentIntent.Metadata["OrderId"]);

                    var order = await _context.Orders
                        .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                        .Include(o => o.User)
                        .FirstOrDefaultAsync(o => o.Id == orderId);

                    if (order != null)
                    {
                        // Mettre à jour le statut de la commande
                        order.Status = "Payée";
                        order.PaidAt = DateTime.UtcNow;
                        order.UpdatedAt = DateTime.UtcNow;
                        order.StripePaymentIntentId = paymentIntent.Id;
                        await _context.SaveChangesAsync();

                        try
                        {
                            // Générer une facture pour la commande complète
                            var vendorIds = order.OrderItems
                                .Where(oi => oi.Product != null)
                                .Select(oi => oi.Product.VendorId)
                                .Distinct()
                                .ToList();

                            foreach (var vendorId in vendorIds)
                            {
                                if (!string.IsNullOrEmpty(vendorId))
                                {
                                    // Vérifier si une facture existe déjà pour ce vendeur et cette commande
                                    var existingInvoice = await _context.Invoices
                                        .FirstOrDefaultAsync(i => i.OrderId == order.Id && i.VendorId == vendorId);

                                    if (existingInvoice == null)
                                    {
                                        var invoice = await _invoiceService.GenerateInvoiceAsync(order, vendorId);
                                        if (invoice != null)
                                        {
                                            invoice.PaymentStatus = "Payée";
                                            invoice.PaidAt = DateTime.UtcNow;
                                            _context.Invoices.Add(invoice);
                                            await _context.SaveChangesAsync();
                                            Console.WriteLine($"Facture créée avec succès pour la commande {order.Id} et le vendeur {vendorId}");
                                        }
                                    }
                                    else
                                    {
                                        existingInvoice.PaymentStatus = "Payée";
                                        existingInvoice.PaidAt = DateTime.UtcNow;
                                        await _context.SaveChangesAsync();
                                        Console.WriteLine($"Facture existante mise à jour pour la commande {order.Id} et le vendeur {vendorId}");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Erreur lors de la génération des factures: {ex.Message}");
                            // Ne pas renvoyer d'erreur pour ne pas bloquer le webhook
                        }

                        await _stripeService.UpdatePaymentIntent(paymentIntent.Id);
                    }
                }

                return Ok();
            }
            catch (StripeException e)
            {
                return BadRequest(e.Message);
            }
        }
    }
} 