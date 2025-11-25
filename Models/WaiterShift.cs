using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace MyApp.Models
{
    public class WaiterShift
    {
        public int Id { get; set; }
        public int WaiterId { get; set; }
        public User Waiter { get; set; } = null!;
    
        // ← ЭТО ГЛАВНОЕ — ОБЯЗАТЕЛЬНАЯ ПРИВЯЗКА К ГЛОБАЛЬНОЙ СМЕНЕ
        public int GlobalShiftId { get; set; }
        public GlobalShift GlobalShift { get; set; } = null!;
    
        public string Name { get; set; } = string.Empty;
        public DateTime StartAt { get; set; }
        public DateTime? EndAt { get; set; }
    
        public List<Order> Orders { get; set; } = new List<Order>();
    
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalRevenue { get; set; }
    
        [Column(TypeName = "decimal(18,2)")]
        public decimal CashRevenue { get; set; }
    
        [Column(TypeName = "decimal(18,2)")]
        public decimal CardRevenue { get; set; }
    
        [NotMapped]
        public int OrdersCount => Orders?.Count ?? 0;
    
        [NotMapped]
        public bool IsActive => EndAt == null;
    
        [NotMapped]
        public TimeSpan Duration => EndAt.HasValue 
            ? EndAt.Value - StartAt 
            : DateTime.Now - StartAt;
    }
}