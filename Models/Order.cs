using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApp.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int TableNumber { get; set; }
        public int CustomersCount { get; set; }
        public string Items { get; set; } = string.Empty;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        
        public int WaiterId { get; set; }
        public User Waiter { get; set; } = null!;
        
        // Обязательная привязка к смене официанта
        public int WaiterShiftId { get; set; }
        public WaiterShift WaiterShift { get; set; } = null!;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Accepted"; // Accepted, Paid, Cancelled
        public string? PaymentMethod { get; set; } // Cash, Card
        public DateTime? PaidAt { get; set; }
    }
}