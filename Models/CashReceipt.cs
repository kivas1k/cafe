using System;

namespace MyApp.Models;

public class CashReceipt
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Cash"; // Cash или Card
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int WaiterId { get; set; }
}