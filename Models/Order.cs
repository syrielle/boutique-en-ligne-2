using System.ComponentModel.DataAnnotations;

namespace ECommerceBoutique.Models
{
    public class Order
    {
        public Order()
        {
            OrderItems = new List<OrderItem>();
        }

        public int Id { get; set; }
        public string UserId { get; set; }
        public User User { get; set; }
        public List<OrderItem> OrderItems { get; set; }
        public string Status { get; set; } = "En attente de confirmation";

        [Required(ErrorMessage = "L'adresse de livraison est requise")]
        public string ShippingAddress { get; set; }

        [Required(ErrorMessage = "La ville est requise")]
        public string ShippingCity { get; set; }

        [Required(ErrorMessage = "Le code postal est requis")]
        public string ShippingPostalCode { get; set; }

        [Required(ErrorMessage = "Le pays est requis")]
        public string ShippingCountry { get; set; }

        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string? StripePaymentIntentId { get; set; }
    }
} 