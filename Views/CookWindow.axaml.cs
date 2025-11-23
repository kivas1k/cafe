using System;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Microsoft.EntityFrameworkCore;
using MyApp.Models;
using MyApp.Services;
using System.Linq;
using System.Threading.Tasks;

namespace MyApp.Views;

public partial class CookWindow : Window
{
    private readonly User _currentUser;
    private readonly AvaloniaList<Order> _orders = new();

    public CookWindow(User user)
    {
        InitializeComponent();
        _currentUser = user;
        OrdersListBox.ItemsSource = _orders;

        // Загружаем при открытии окна
        this.Opened += async (_, __) => await LoadDataAsync();

        // Правильный способ: авто-обновление каждые 5 секунд
        DispatcherTimer.Run(() =>
        {
            _ = LoadDataAsync(); // fire-and-forget — безопасно
            return true;
        }, TimeSpan.FromSeconds(5));
    }

    private async Task LoadDataAsync()
    {
        using var db = new AppDbContext();
        var list = await db.Orders
            .Where(o => o.Status == "Accepted" || o.Status == "Cooking" || o.Status == "Ready")
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _orders.Clear();
            _orders.AddRange(list);
        });
    }

    private async void OrdersListBox_DoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (OrdersListBox.SelectedItem is not Order order)
            return;

        var card = new OrderCardWindow(order);
        card.Closed += async (_, __) => await LoadDataAsync();
        await card.ShowDialog(this);
    }
}