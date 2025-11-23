using System;

namespace MyApp.Models;

public class Order
{
    public int Id { get; set; }
    public int TableNumber { get; set; }
    public int CustomersCount { get; set; } = 1;
    public string Items { get; set; } = string.Empty;
    public string Status { get; set; } = "New";           // ← было "Accepted"
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? PaidAt { get; set; }                 // ← добавлено
    
    public int WaiterId { get; set; }
    public string? PaymentMethod { get; set; }            // Cash или Card
    
    public decimal? TotalAmount { get; set; }             // ← САМОЕ ВАЖНОЕ! Сумма заказа
}