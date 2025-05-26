using Microsoft.EntityFrameworkCore;
using ECommerceBoutique.Data;
using ECommerceBoutique.Models;

namespace ECommerceBoutique.Services
{
    public class InvoiceService
    {
        private readonly ApplicationDbContext _context;

        public InvoiceService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Invoice> GenerateInvoiceAsync(Order order, string vendorId)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));
            if (string.IsNullOrEmpty(vendorId))
                throw new ArgumentNullException(nameof(vendorId));

            var vendor = await _context.Users.FindAsync(vendorId);
            if (vendor == null)
                throw new InvalidOperationException($"Vendor with ID {vendorId} not found");

            var vendorItems = order.OrderItems
                .Where(oi => oi.Product != null && oi.Product.VendorId == vendorId)
                .ToList();

            if (!vendorItems.Any())
                throw new InvalidOperationException($"No items found for vendor {vendorId} in order {order.Id}");

            var totalAmount = vendorItems.Sum(item => item.Quantity * item.UnitPrice);
            var vendorTotal = totalAmount * 0.9M; // 90% du total (10% de commission)
            
            var invoice = new Invoice
            {
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{order.Id}-{vendorId.Substring(0, 4)}",
                InvoiceDate = DateTime.UtcNow,
                OrderId = order.Id,
                Order = order,
                BuyerId = order.UserId,
                Buyer = order.User,
                VendorId = vendorId,
                Vendor = vendor,
                BuyerName = $"{order.User.FirstName} {order.User.LastName}",
                VendorName = vendor.CompanyName,
                BuyerAddress = $"{order.ShippingAddress}, {order.ShippingPostalCode} {order.ShippingCity}, {order.ShippingCountry}",
                VendorAddress = $"{vendor.Address}, {vendor.PostalCode} {vendor.City}, {vendor.Country}",
                TotalAmount = totalAmount,
                VendorTotal = vendorTotal,
                PaymentStatus = order.Status,
                PaidAt = order.PaidAt,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                // Ajout de la facture
                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();

                // Ensuite, création et ajoute des éléments de la facture
                foreach (var item in vendorItems)
                {
                    var invoiceItem = new InvoiceItem
                    {
                        InvoiceId = invoice.Id,
                        Invoice = invoice,
                        ProductId = item.ProductId,
                        ProductName = item.Product.Name,
                        UnitPrice = item.UnitPrice,
                        Quantity = item.Quantity,
                        Subtotal = item.Quantity * item.UnitPrice
                    };
                    _context.Add(invoiceItem);
                }
                await _context.SaveChangesAsync();

                return invoice;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving invoice for order {order.Id} and vendor {vendorId}: {ex.Message}", ex);
            }
        }

        public async Task<List<Invoice>> GetBuyerInvoicesAsync(string userId)
        {
            return await _context.Invoices
                .Include(i => i.Order)
                .Include(i => i.Vendor)
                .Where(i => i.Order.UserId == userId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Invoice>> GetVendorInvoicesAsync(string vendorId)
        {
            return await _context.Invoices
                .Include(i => i.Order)
                .Include(i => i.Order.User)
                .Where(i => i.VendorId == vendorId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalSpentAsync(string userId)
        {
            return await _context.Invoices
                .Include(i => i.Order)
                .Where(i => i.Order.UserId == userId && i.PaymentStatus == "Payée")
                .SumAsync(i => i.TotalAmount);
        }

        public async Task<decimal> GetTotalEarningsAsync(string vendorId)
        {
            return await _context.Invoices
                .Where(i => i.VendorId == vendorId && i.PaymentStatus == "Payée")
                .SumAsync(i => i.VendorTotal);
        }
    }
} 