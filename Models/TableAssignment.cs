using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApp.Models
{
    public class TableAssignment
    {
        public int Id { get; set; }
        
        public int TableNumber { get; set; }
        public int WaiterId { get; set; }
        public User Waiter { get; set; } = null!;
        
        public int GlobalShiftId { get; set; }
        public GlobalShift GlobalShift { get; set; } = null!;
        
        public DateTime AssignedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
        
        [NotMapped]
        public string WaiterName => Waiter?.FullName ?? "Неизвестно";
    }
}