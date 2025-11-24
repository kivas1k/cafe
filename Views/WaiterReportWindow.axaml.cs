using Avalonia.Controls;
using MyApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using MyApp.Services;

namespace MyApp.Views
{
    public partial class WaiterReportWindow : Window
    {
        private readonly int _waiterId;
        private readonly int _waiterShiftId;

        public WaiterReportWindow(int waiterId, int waiterShiftId)
        {
            InitializeComponent();
            _waiterId = waiterId;
            _waiterShiftId = waiterShiftId;
            this.Opened += async (_, __) => await LoadReportAsync();
        }

        private async Task LoadReportAsync()
        {
            using var db = new AppDbContext();
            var shift = await db.WaiterShifts
                .FirstOrDefaultAsync(s => s.Id == _waiterShiftId && s.WaiterId == _waiterId);

            if (shift == null)
            {
                TitleText.Text = "Смена не найдена";
                return;
            }

            // Получаем заказы через отдельный запрос к Orders
            var orders = await db.Orders
                .Where(o => o.WaiterShiftId == _waiterShiftId)
                .OrderBy(o => o.CreatedAt)
                .ToListAsync();

            TitleText.Text = "Отчёт официанта";
            ShiftInfoText.Text = $"Смена: {shift.StartAt:dd.MM.yyyy HH:mm} — {(shift.EndAt?.ToString("dd.MM.yyyy HH:mm") ?? "Активна")}";

            ContentPanel.Children.Clear();

            ContentPanel.Children.Add(new TextBlock { Text = $"Всего заказов: {orders.Count}" });
            var paid = orders.Where(o => o.Status == "Paid").ToList();
            ContentPanel.Children.Add(new TextBlock { Text = $"Оплачено: {paid.Count}" });

            var cash = paid.Where(o => o.PaymentMethod == "Cash").Sum(o => o.TotalAmount);
            var card = paid.Where(o => o.PaymentMethod == "Card").Sum(o => o.TotalAmount);
            ContentPanel.Children.Add(new TextBlock { Text = $"Наличные: {cash:C}" });
            ContentPanel.Children.Add(new TextBlock { Text = $"Карта: {card:C}" });
            ContentPanel.Children.Add(new TextBlock { Text = $"Итого выручка: {(cash + card):C}", FontWeight = Avalonia.Media.FontWeight.Bold });

            ContentPanel.Children.Add(new Separator());
            foreach (var o in orders)
            {
                var t = new TextBlock { Text = $"#{o.Id} | {o.CreatedAt:HH:mm} | Стол {o.TableNumber} | {o.TotalAmount:C} | {o.Status}" };
                ContentPanel.Children.Add(t);
            }
        }

        private void Close_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();
    }
}