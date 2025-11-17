using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MyApp.Models;
using MyApp.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;

namespace MyApp.Views
{
    public partial class WaiterWindow : Window
    {
        private readonly User _currentUser;
        private readonly AvaloniaList<Order> _orders = new();

        public WaiterWindow(User user)
        {
            InitializeComponent();
            _currentUser = user;
            OrdersListBox.ItemsSource = _orders;
            this.Opened += async (s, e) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            await Task.Run(() =>
            {
                using var db = new AppDbContext();
                var orders = db.Orders
                    .Where(o => o.WaiterId == _currentUser.Id)
                    .ToList();

                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    OrdersListBox.ItemsSource = orders;
                });
            });
        }

        private async void CreateOrder_Click(object? sender, RoutedEventArgs e)
        {
            var newOrder = new Order
            {
                TableNumber = 1, // Пример
                CustomersCount = 2,
                Items = "Кофе, торт", // Пример
                WaiterId = _currentUser.Id
            };
            using var db = new AppDbContext();
            db.Orders.Add(newOrder);
            await db.SaveChangesAsync();
            await LoadDataAsync();
            await MessageBox.Show(this, "Заказ создан.");
        }

       private async void ChangeStatus_Click(object? sender, RoutedEventArgs e)
        {
            if (OrdersListBox.SelectedItem is Order selectedOrder)
            {
                selectedOrder.Status = "Accepted"; // Пример
                using var db = new AppDbContext();
                db.Orders.Update(selectedOrder);
                await db.SaveChangesAsync();
                await LoadDataAsync();
                await MessageBox.Show(this, "Статус изменён.");
            }
        }

        private async void PayOrder_Click(object? sender, RoutedEventArgs e)
        {
            if (OrdersListBox.SelectedItem is Order selectedOrder)
            {
                selectedOrder.Status = "Paid";
                selectedOrder.PaymentMethod = "Cash"; // Пример
                using var db = new AppDbContext();
                db.Orders.Update(selectedOrder);
                await db.SaveChangesAsync();
                await LoadDataAsync();
                await MessageBox.Show(this, $"Заказ {selectedOrder.Id} оплачен ({selectedOrder.PaymentMethod}).");
            }
        }

        private async void GenerateReport_Click(object? sender, RoutedEventArgs e)
        {
            using var db = new AppDbContext();
            var waiterOrders = await db.Orders.Where(o => o.WaiterId == _currentUser.Id).ToListAsync();

            var reportWindow = new Window { Title = "Отчёт официанта", Width = 400, Height = 300 };
            var stack = new StackPanel { Spacing = 5, Margin = new Thickness(10) };
            foreach (var order in waiterOrders)
            {
                stack.Children.Add(new TextBlock { Text = $"Заказ {order.Id}: {order.Items}, Статус: {order.Status}" });
            }
            reportWindow.Content = new ScrollViewer { Content = stack };
            await reportWindow.ShowDialog(this);
        }
    }
}