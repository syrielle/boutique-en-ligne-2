using System.ComponentModel.DataAnnotations;

namespace ECommerceBoutique.Models
{
    public class Cart
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public User User { get; set; }
        public List<CartItem> Items { get; set; } = new List<CartItem>();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public decimal Total => Items.Sum(item => item.Subtotal);
    }
} 