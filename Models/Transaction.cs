namespace ECommerceBoutique.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public string TransactionId { get; set; } // ID de transaction Stripe
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public DateTime TransactionDate { get; set; }
        
        // Relations
        public int OrderId { get; set; }
        public Order Order { get; set; }
        
        public string UserId { get; set; }
        public User User { get; set; }
        
        public int? InvoiceId { get; set; }
        public Invoice Invoice { get; set; }
    }
} 