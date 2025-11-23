using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Layout;
using MyApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using MyApp.Services;

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

            this.Opened += async (_, __) => await LoadOrdersAsync();
        }

        private async Task LoadOrdersAsync()
        {
            using var db = new AppDbContext();
            var today = DateTime.Today;

            var orders = await db.Orders
                .Where(o => o.WaiterId == _currentUser.Id && o.CreatedAt.Date == today)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            _orders.Clear();
            _orders.AddRange(orders);
        }

        private async void CreateOrder_Click(object? sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Title = "Новый заказ",
                Width = 380,
                Height = 440,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var stack = new StackPanel { Margin = new Thickness(20), Spacing = 12 };

            stack.Children.Add(new TextBlock { Text = "Номер стола:", FontWeight = FontWeight.SemiBold });
            var tableBox = new NumericUpDown { Value = 1, Minimum = 1, Maximum = 50 };
            stack.Children.Add(tableBox);

            stack.Children.Add(new TextBlock { Text = "Количество гостей:", FontWeight = FontWeight.SemiBold });
            var guestsBox = new NumericUpDown { Value = 2, Minimum = 1, Maximum = 10 };
            stack.Children.Add(guestsBox);

            stack.Children.Add(new TextBlock { Text = "Состав заказа:", FontWeight = FontWeight.SemiBold });
            var itemsBox = new TextBox { Height = 80, AcceptsReturn = true, Text = "Кофе, пирожное" };
            stack.Children.Add(itemsBox);

            stack.Children.Add(new TextBlock { Text = "Сумма (₽):", FontWeight = FontWeight.SemiBold });
            var amountBox = new NumericUpDown { Value = 800, Minimum = 0, Increment = 50 };
            stack.Children.Add(amountBox);

            var btn = new Button
            {
                Content = "Создать заказ",
                Background = Brushes.Green,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 20, 0, 0)
            };

            btn.Click += async (_, __) =>
            {
                var order = new Order
                {
                    TableNumber = (int)tableBox.Value,
                    CustomersCount = (int)guestsBox.Value,
                    Items = itemsBox.Text ?? "",
                    TotalAmount = (decimal)amountBox.Value,
                    WaiterId = _currentUser.Id,
                    CreatedAt = DateTime.Now,
                    Status = "New"
                };

                using var db = new AppDbContext();
                db.Orders.Add(order);
                await db.SaveChangesAsync();

                dialog.Close();
                await LoadOrdersAsync();
                await MessageBox.Show(this, $"Заказ №{order.Id} успешно создан!");
            };

            stack.Children.Add(btn);
            dialog.Content = stack;
            await dialog.ShowDialog(this);
        }

        private async void ChangeStatus_Click(object? sender, RoutedEventArgs e)
        {
            if (OrdersListBox.SelectedItem is not Order order) return;
            if (order.Status == "Paid")
            {
                await MessageBox.Show(this, "Оплаченный заказ нельзя менять.");
                return;
            }

            order.Status = order.Status == "New" ? "Accepted" : "New";

            using var db = new AppDbContext();
            db.Orders.Update(order);
            await db.SaveChangesAsync();
            await LoadOrdersAsync();
        }

        private async void PayOrder_Click(object? sender, RoutedEventArgs e)
        {
            if (OrdersListBox.SelectedItem is not Order order || order.Status == "Paid")
            {
                await MessageBox.Show(this, "Выберите неоплаченный заказ.");
                return;
            }

            var win = new Window
            {
                Title = $"Оплата заказа №{order.Id}",
                Width = 360,
                Height = 240,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var stack = new StackPanel { Margin = new Thickness(20), Spacing = 15 };

            stack.Children.Add(new TextBlock
            {
                Text = $"Сумма к оплате: {order.TotalAmount:C}",
                FontSize = 18,
                FontWeight = FontWeight.Bold
            });

            var combo = new ComboBox { ItemsSource = new[] { "Наличные", "Карта" }, SelectedIndex = 0 };
            stack.Children.Add(new TextBlock { Text = "Способ оплаты:" });
            stack.Children.Add(combo);

            var payBtn = new Button
            {
                Content = "Принять оплату",
                Background = Brushes.Green,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 10, 0, 0)
            };

            payBtn.Click += async (_, __) =>
            {
                var method = combo.SelectedIndex == 0 ? "Cash" : "Card";
                var methodText = combo.SelectedIndex == 0 ? "Наличные" : "Карта";

                order.Status = "Paid";
                order.PaymentMethod = method;
                order.PaidAt = DateTime.Now;

                var receipt = new CashReceipt
                {
                    OrderId = order.Id,
                    Amount = order.TotalAmount ?? 0,
                    PaymentMethod = method,
                    CreatedAt = DateTime.Now,
                    WaiterId = _currentUser.Id
                };

                using var db = new AppDbContext();
                db.Orders.Update(order);
                db.CashReceipts.Add(receipt);
                await db.SaveChangesAsync();

                win.Close();
                await LoadOrdersAsync();
                await MessageBox.Show(this, $"Заказ оплачен!\nПКО №{receipt.Id}\nСпособ: {methodText}");
            };

            stack.Children.Add(payBtn);
            win.Content = stack;
            await win.ShowDialog(this);
        }

        private async void GenerateReport_Click(object? sender, RoutedEventArgs e)
        {
            using var db = new AppDbContext();
            var today = DateTime.Today;

            var orders = await db.Orders
                .Where(o => o.WaiterId == _currentUser.Id && o.CreatedAt.Date == today)
                .ToListAsync();

            var paid = orders.Where(o => o.Status == "Paid").ToList();
            var cash = paid.Where(o => o.PaymentMethod == "Cash").Sum(o => o.TotalAmount ?? 0);
            var card = paid.Where(o => o.PaymentMethod == "Card").Sum(o => o.TotalAmount ?? 0);
            var total = cash + card;

            var win = new Window { Title = "Отчёт по смене", Width = 520, Height = 460 };
            var stack = new StackPanel { Margin = new Thickness(25), Spacing = 12 };

            stack.Children.Add(new TextBlock { Text = "ОТЧЁТ ОФИЦИАНТА", FontSize = 22, FontWeight = FontWeight.Bold, HorizontalAlignment = HorizontalAlignment.Center });
            stack.Children.Add(new TextBlock { Text = _currentUser.FullName, HorizontalAlignment = HorizontalAlignment.Center });
            stack.Children.Add(new TextBlock { Text = today.ToString("dd.MM.yyyy"), HorizontalAlignment = HorizontalAlignment.Center });
            stack.Children.Add(new TextBlock { Text = new string('═', 35), HorizontalAlignment = HorizontalAlignment.Center });
            stack.Children.Add(new TextBlock { Text = $"Заказов за смену: {orders.Count}" });
            stack.Children.Add(new TextBlock { Text = $"Оплачено заказов: {paid.Count}" });
            stack.Children.Add(new TextBlock { Text = $"Наличные: {cash:C}" });
            stack.Children.Add(new TextBlock { Text = $"Карта: {card:C}" });
            stack.Children.Add(new TextBlock { Text = $"ИТОГО ВЫРУЧКА: {total:C}", FontSize = 18, FontWeight = FontWeight.Bold });

            win.Content = new ScrollViewer { Content = stack };
            await win.ShowDialog(this);
        }
    }
}
