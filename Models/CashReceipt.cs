using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApp.Models
{
    public class CashReceipt
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        
        public string PaymentMethod { get; set; } = string.Empty; // Cash, Card
        public int WaiterId { get; set; }
        public User Waiter { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}