using System;

namespace MyApp.Models;

public class Order
{
    public int Id { get; set; }
    public int TableNumber { get; set; }
    public int CustomersCount { get; set; } = 1;
    public string Items { get; set; } = string.Empty;
    public string Status { get; set; } = "Accepted";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int WaiterId { get; set; }
    public string? PaymentMethod { get; set; }
}