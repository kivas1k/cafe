using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MyApp.Models;
using MyApp.Services;

namespace MyApp.Views;

public partial class OrderCardWindow : Window
{
    private readonly Order _order;

    public OrderCardWindow(Order order)
    {
        InitializeComponent();
        _order = order;
        DataContext = order;
    }

    private async void SetCooking_Click(object? sender, RoutedEventArgs e)
    {
        _order.Status = "Cooking";
        await UpdateOrderAsync();
        await MessageBox.Show(this, "Статус изменён: Готовится");
        Close();
    }

    private async void SetReady_Click(object? sender, RoutedEventArgs e)
    {
        _order.Status = "Ready";
        await UpdateOrderAsync();
        await MessageBox.Show(this, "Заказ готов!");
        Close();
    }

    private static async Task UpdateOrderAsync(Order order)
    {
        using var db = new AppDbContext();
        db.Orders.Update(order);
        await db.SaveChangesAsync();
    }

    private Task UpdateOrderAsync() => UpdateOrderAsync(_order);
}