using Avalonia.Controls;
using Avalonia.Interactivity;
using MyApp.Models;
using MyApp.Services;

namespace MyApp.Views
{
    public partial class OrderSimpleEditWindow : Window
    {
        private readonly Order _order;
        public bool WasSaved { get; private set; } = false;

        public OrderSimpleEditWindow(Order order)
        {
            InitializeComponent();
            _order = order;

            TableNumberText.Text = $"Стол {order.TableNumber}";
            CustomersCountBox.Text = order.CustomersCount.ToString();
            ItemsBox.Text = order.Items;
            TotalAmountBox.Text = order.TotalAmount.ToString("F2");
        }

        private void Save_Click(object? sender, RoutedEventArgs e)
        {
            if (!int.TryParse(CustomersCountBox.Text, out int guests) || guests <= 0)
            {
                MessageBox.Show(this, "Введите нормальное количество гостей");
                return;
            }

            if (!decimal.TryParse(TotalAmountBox.Text, out decimal amount) || amount < 0)
            {
                MessageBox.Show(this, "Введите правильную сумму");
                return;
            }

            if (string.IsNullOrWhiteSpace(ItemsBox.Text.Trim()))
            {
                MessageBox.Show(this, "Напишите, что заказали");
                return;
            }

            // Сохраняем изменения
            _order.CustomersCount = guests;
            _order.Items = ItemsBox.Text.Trim();
            _order.TotalAmount = amount;

            WasSaved = true;
            Close();
        }

        private void Cancel_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}