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
        private WaiterShift? _activeWaiterShift;

        public WaiterWindow(User user)
        {
            try
            {
                InitializeComponent();
                _currentUser = user;
                OrdersListBox.ItemsSource = _orders;
                this.Opened += async (_, __) => await InitializeShiftAndLoadAsync();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(this, $"Ошибка при создании окна официанта: {ex.Message}");
            }
        }

        private async Task InitializeShiftAndLoadAsync()
        {
            await LoadActiveWaiterShiftAsync();
            await LoadOrdersAsync();
            UpdateShiftInfoUi();
        }

        private async Task LoadActiveWaiterShiftAsync()
        {
            try
            {
                using var db = new AppDbContext();
                _activeWaiterShift = await db.WaiterShifts
                    .Include(ws => ws.Orders)
                    .FirstOrDefaultAsync(s => s.WaiterId == _currentUser.Id && s.EndAt == null);
            }
            catch (Exception ex)
            {
                await MessageBox.Show(this, $"Ошибка при загрузке смены: {ex.Message}");
            }
        }

        private void UpdateShiftInfoUi()
        {
            if (_activeWaiterShift == null)
            {
                ShiftInfoText.Text = "Смена: не активна";
                ToggleShiftBtn.Content = "Открыть смену";
                ToggleShiftBtn.Background = Brushes.Green;
                CreateOrderBtn.IsEnabled = false;
                PayOrderBtn.IsEnabled = false;
            }
            else
            {
                var duration = _activeWaiterShift.Duration;
                ShiftInfoText.Text = $"Смена: {_activeWaiterShift.StartAt:HH:mm} (Открыта {duration.Hours}ч {duration.Minutes}м)";
                ToggleShiftBtn.Content = "Закрыть смену";
                ToggleShiftBtn.Background = Brushes.OrangeRed;
                CreateOrderBtn.IsEnabled = true;
                PayOrderBtn.IsEnabled = true;
            }
        }

        private async Task LoadOrdersAsync()
        {
            if (_activeWaiterShift == null)
            {
                _orders.Clear();
                return;
            }

            using var db = new AppDbContext();
            var orders = await db.Orders
                .Where(o => o.WaiterShiftId == _activeWaiterShift.Id)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            _orders.Clear();
            _orders.AddRange(orders);
        }

        private async Task<bool> IsUserInTodayGlobalShiftAsync()
        {
            try
            {
                using var db = new AppDbContext();
                var today = DateTime.Today;
                
                // Ищем активную глобальную смену на сегодня
                var todayGlobalShift = await db.GlobalShifts
                    .FirstOrDefaultAsync(gs => gs.Date.Date == today && gs.IsActive);

                if (todayGlobalShift == null)
                {
                    await MessageBox.Show(this, 
                        "На сегодня нет активной глобальной смены.\nОбратитесь к администратору.");
                    return false;
                }

                // Проверяем, есть ли текущий пользователь в списке сотрудников глобальной смены
                if (!todayGlobalShift.EmployeeIds.Contains(_currentUser.Id))
                {
                    await MessageBox.Show(this, 
                        $"Вы не назначены в смену на сегодня.\n" +
                        $"Смена: {todayGlobalShift.Name}\n" +
                        $"Обратитесь к администратору для добавления в смену.");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                await MessageBox.Show(this, $"Ошибка при проверке смены: {ex.Message}");
                return false;
            }
        }

        private async void ToggleShift_Click(object? sender, RoutedEventArgs e)
        {
            using var db = new AppDbContext();

            if (_activeWaiterShift == null)
            {
                // ОТКРЫТИЕ НОВОЙ СМЕНЫ - проверяем наличие в глобальной смене
                bool canOpenShift = await IsUserInTodayGlobalShiftAsync();
                if (!canOpenShift)
                {
                    return; // Нельзя открыть смену - пользователь не в глобальной смене
                }

                // Получаем глобальную смену для привязки
                var today = DateTime.Today;
                var todayGlobalShift = await db.GlobalShifts
                    .FirstOrDefaultAsync(gs => gs.Date.Date == today && gs.IsActive);

                if (todayGlobalShift == null)
                {
                    await MessageBox.Show(this, "Не найдена активная глобальная смена на сегодня.");
                    return;
                }

                var newShift = new WaiterShift
                {
                    WaiterId = _currentUser.Id,
                    Name = $"Смена {_currentUser.FullName} {DateTime.Now:dd.MM.yyyy HH:mm}",
                    StartAt = DateTime.Now,
                    GlobalShiftId = todayGlobalShift.Id // Привязываем к глобальной смене
                };

                db.WaiterShifts.Add(newShift);
                await db.SaveChangesAsync();
                
                _activeWaiterShift = newShift;
                await MessageBox.Show(this, "Смена открыта!");
            }
            else
            {
                // ЗАКРЫТИЕ ТЕКУЩЕЙ СМЕНЫ
                _activeWaiterShift.EndAt = DateTime.Now;
                
                // Рассчитываем итоги смены
                var shiftOrders = await db.Orders
                    .Where(o => o.WaiterShiftId == _activeWaiterShift.Id && o.Status == "Paid")
                    .ToListAsync();

                _activeWaiterShift.TotalRevenue = shiftOrders.Sum(o => o.TotalAmount);
                _activeWaiterShift.CashRevenue = shiftOrders
                    .Where(o => o.PaymentMethod == "Cash")
                    .Sum(o => o.TotalAmount);
                _activeWaiterShift.CardRevenue = shiftOrders
                    .Where(o => o.PaymentMethod == "Card")
                    .Sum(o => o.TotalAmount);

                db.WaiterShifts.Update(_activeWaiterShift);
                await db.SaveChangesAsync();
                
                await MessageBox.Show(this, 
                    $"Смена закрыта!\n" +
                    $"Заказов: {shiftOrders.Count}\n" +
                    $"Выручка: {_activeWaiterShift.TotalRevenue:C}");
                
                _activeWaiterShift = null;
            }

            await LoadOrdersAsync();
            UpdateShiftInfoUi();
        }

        private async void CreateOrder_Click(object? sender, RoutedEventArgs e)
        {
            if (_activeWaiterShift == null)
            {
                await MessageBox.Show(this, "Сначала откройте смену!");
                return;
            }

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
                    WaiterShiftId = _activeWaiterShift!.Id,
                    CreatedAt = DateTime.Now,
                    Status = "Accepted"
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

        private async void OrdersListBox_DoubleTapped(object? sender, RoutedEventArgs e)
        {
            if (OrdersListBox.SelectedItem is not Order order) return;
            
            var orderEditWindow = new OrderEditWindow(order, async () => await LoadOrdersAsync());
            await orderEditWindow.ShowDialog(this);
        }

        private async void PayOrder_Click(object? sender, RoutedEventArgs e)
        {
            if (OrdersListBox.SelectedItem is not Order order)
            {
                await MessageBox.Show(this, "Выберите заказ для оплаты.");
                return;
            }

            if (order.Status == "Paid")
            {
                await MessageBox.Show(this, "Заказ уже оплачен.");
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
                    Amount = order.TotalAmount,
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

        private async void OpenShiftReport_Click(object? sender, RoutedEventArgs e)
        {
            if (_activeWaiterShift == null)
            {
                await MessageBox.Show(this, "Нет активной смены для отчёта.");
                return;
            }

            var reportWin = new WaiterReportWindow(_currentUser.Id, _activeWaiterShift.Id);
            await reportWin.ShowDialog(this);
        }
    }
}