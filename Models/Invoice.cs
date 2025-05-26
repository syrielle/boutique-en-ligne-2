using System.ComponentModel.DataAnnotations;

namespace ECommerceBoutique.Models
{
    public class Invoice
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime InvoiceDate { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; }
        public string BuyerId { get; set; }
        public User Buyer { get; set; }
        public string VendorId { get; set; }
        public User Vendor { get; set; }
        public string BuyerName { get; set; }
        public string VendorName { get; set; }
        public string BuyerAddress { get; set; }
        public string VendorAddress { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal VendorTotal { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    }
} 