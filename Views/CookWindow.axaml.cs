using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MyApp.Models;
using MyApp.Services;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace MyApp.Views
{
    public partial class CookWindow : Window
    {
        private readonly User _currentUser;
        private readonly AvaloniaList<Order> _orders = new();

        public CookWindow(User user)
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
                    .Where(o => o.Status == "Accepted" || o.Status == "Cooking")
                    .ToList();

                // Возвращаемся в UI-поток и обновляем список
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    OrdersListBox.ItemsSource = orders;
                });
            });
        }

        private async void SetCooking_Click(object? sender, RoutedEventArgs e)
        {
            if (OrdersListBox.SelectedItem is Order selectedOrder)
            {
                selectedOrder.Status = "Cooking";
                using var db = new AppDbContext();
                db.Orders.Update(selectedOrder);
                await db.SaveChangesAsync();
                await LoadDataAsync();
                await MessageBox.Show(this, "Статус: Готовится.");
            }
        }

        private async void SetReady_Click(object? sender, RoutedEventArgs e)
        {
            if (OrdersListBox.SelectedItem is Order selectedOrder)
            {
                selectedOrder.Status = "Ready";
                using var db = new AppDbContext();
                db.Orders.Update(selectedOrder);
                await db.SaveChangesAsync();
                await LoadDataAsync();
                await MessageBox.Show(this, "Статус: Готово.");
            }
        }
    }
}