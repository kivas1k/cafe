using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MyApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using MyApp.Services;

namespace MyApp.Views
{
    public partial class OrderEditWindow : Window
    {
        private readonly Order _order;
        private readonly Action? _onSaved;

        public OrderEditWindow(Order order, Action? onSaved = null)
        {
            InitializeComponent();
            _order = order;
            _onSaved = onSaved;

            InitializeControls();
        }

        private void InitializeControls()
        {
            // Находим элементы управления
            var statusComboBox = this.Find<ComboBox>("StatusComboBox");
            var paymentMethodComboBox = this.Find<ComboBox>("PaymentMethodComboBox");
            
            // Заполняем комбобоксы
            statusComboBox.ItemsSource = new[] { "Accepted", "Paid", "Cancelled" };
            paymentMethodComboBox.ItemsSource = new[] { "Cash", "Card" };

            LoadOrderData();
        }

        private void LoadOrderData()
        {
            // Находим элементы управления
            var titleText = this.Find<TextBlock>("TitleText");
            var createdAtText = this.Find<TextBlock>("CreatedAtText");
            var tableNumberBox = this.Find<TextBox>("TableNumberBox");
            var customersCountBox = this.Find<TextBox>("CustomersCountBox");
            var itemsBox = this.Find<TextBox>("ItemsBox");
            var amountBox = this.Find<TextBox>("AmountBox");
            var statusComboBox = this.Find<ComboBox>("StatusComboBox");
            var paymentMethodPanel = this.Find<StackPanel>("PaymentMethodPanel");
            var paymentMethodComboBox = this.Find<ComboBox>("PaymentMethodComboBox");
            var payButton = this.Find<Button>("PayButton");

            // Заполняем поля данными заказа
            titleText.Text = $"Заказ №{_order.Id}";
            tableNumberBox.Text = _order.TableNumber.ToString();
            customersCountBox.Text = _order.CustomersCount.ToString();
            itemsBox.Text = _order.Items ?? "";
            amountBox.Text = _order.TotalAmount.ToString("F2");
            createdAtText.Text = $"Создан: {_order.CreatedAt:HH:mm • dd.MM.yyyy}";
            
            // Устанавливаем статус
            statusComboBox.SelectedItem = _order.Status;
            
            // Если заказ оплачен, показываем способ оплаты
            if (_order.Status == "Paid")
            {
                paymentMethodPanel.IsVisible = true;
                paymentMethodComboBox.SelectedItem = _order.PaymentMethod ?? "Cash";
                payButton.IsEnabled = false; // Нельзя оплатить уже оплаченный заказ
            }

            // Обновляем видимость кнопки оплаты
            UpdatePayButtonVisibility();
        }

        private void UpdatePayButtonVisibility()
        {
            var payButton = this.Find<Button>("PayButton");
            payButton.IsVisible = _order.Status != "Paid";
        }

        private async void Save_Click(object? sender, RoutedEventArgs e)
        {
            if (!ValidateInputs()) return;

            try
            {
                // Находим элементы управления
                var tableNumberBox = this.Find<TextBox>("TableNumberBox");
                var customersCountBox = this.Find<TextBox>("CustomersCountBox");
                var itemsBox = this.Find<TextBox>("ItemsBox");
                var amountBox = this.Find<TextBox>("AmountBox");
                var statusComboBox = this.Find<ComboBox>("StatusComboBox");
                var paymentMethodComboBox = this.Find<ComboBox>("PaymentMethodComboBox");

                // Обновляем данные заказа
                _order.TableNumber = int.Parse(tableNumberBox.Text ?? "1");
                _order.CustomersCount = int.Parse(customersCountBox.Text ?? "1");
                _order.Items = itemsBox.Text ?? "";
                _order.TotalAmount = decimal.Parse(amountBox.Text ?? "0");
                _order.Status = statusComboBox.SelectedItem as string ?? "Accepted";

                // Если статус "Paid", обновляем способ оплаты
                if (_order.Status == "Paid" && string.IsNullOrEmpty(_order.PaymentMethod))
                {
                    _order.PaymentMethod = paymentMethodComboBox.SelectedItem as string ?? "Cash";
                    _order.PaidAt = DateTime.Now;
                }

                using var db = new AppDbContext();
                db.Orders.Update(_order);
                await db.SaveChangesAsync();

                _onSaved?.Invoke();
                await MessageBox.Show(this, "Заказ успешно сохранён!");
                Close();
            }
            catch (Exception ex)
            {
                await MessageBox.Show(this, $"Ошибка сохранения: {ex.Message}");
            }
        }

        private async void Pay_Click(object? sender, RoutedEventArgs e)
        {
            if (!ValidateInputs()) return;

            try
            {
                // Находим элементы управления
                var tableNumberBox = this.Find<TextBox>("TableNumberBox");
                var customersCountBox = this.Find<TextBox>("CustomersCountBox");
                var itemsBox = this.Find<TextBox>("ItemsBox");
                var amountBox = this.Find<TextBox>("AmountBox");
                var paymentMethodComboBox = this.Find<ComboBox>("PaymentMethodComboBox");

                // Обновляем базовые данные
                _order.TableNumber = int.Parse(tableNumberBox.Text ?? "1");
                _order.CustomersCount = int.Parse(customersCountBox.Text ?? "1");
                _order.Items = itemsBox.Text ?? "";
                _order.TotalAmount = decimal.Parse(amountBox.Text ?? "0");
                
                // Устанавливаем статус оплаты
                _order.Status = "Paid";
                _order.PaymentMethod = paymentMethodComboBox.SelectedItem as string ?? "Cash";
                _order.PaidAt = DateTime.Now;

                // Создаем ПКО
                var receipt = new CashReceipt
                {
                    OrderId = _order.Id,
                    Amount = _order.TotalAmount,
                    PaymentMethod = _order.PaymentMethod,
                    CreatedAt = DateTime.Now,
                    WaiterId = _order.WaiterId
                };

                using var db = new AppDbContext();
                db.Orders.Update(_order);
                db.CashReceipts.Add(receipt);
                await db.SaveChangesAsync();

                _onSaved?.Invoke();
                await MessageBox.Show(this, $"Заказ оплачен!\nПКО №{receipt.Id}");
                Close();
            }
            catch (Exception ex)
            {
                await MessageBox.Show(this, $"Ошибка оплаты: {ex.Message}");
            }
        }

        private bool ValidateInputs()
        {
            var tableNumberBox = this.Find<TextBox>("TableNumberBox");
            var customersCountBox = this.Find<TextBox>("CustomersCountBox");
            var amountBox = this.Find<TextBox>("AmountBox");
            var itemsBox = this.Find<TextBox>("ItemsBox");

            if (!int.TryParse(tableNumberBox.Text, out int table) || table <= 0)
            {
                _ = MessageBox.Show(this, "Введите корректный номер стола");
                return false;
            }

            if (!int.TryParse(customersCountBox.Text, out int guests) || guests <= 0)
            {
                _ = MessageBox.Show(this, "Введите корректное количество гостей");
                return false;
            }

            if (!decimal.TryParse(amountBox.Text, out decimal amount) || amount <= 0)
            {
                _ = MessageBox.Show(this, "Введите корректную сумму");
                return false;
            }

            if (string.IsNullOrWhiteSpace(itemsBox.Text))
            {
                _ = MessageBox.Show(this, "Введите состав заказа");
                return false;
            }

            return true;
        }

        private void StatusComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var statusComboBox = this.Find<ComboBox>("StatusComboBox");
            var paymentMethodPanel = this.Find<StackPanel>("PaymentMethodPanel");
            var paymentMethodComboBox = this.Find<ComboBox>("PaymentMethodComboBox");

            // Если статус изменен на "Paid", показываем панель способа оплаты
            if (statusComboBox.SelectedItem?.ToString() == "Paid")
            {
                paymentMethodPanel.IsVisible = true;
                paymentMethodComboBox.SelectedItem = "Cash";
            }
            else
            {
                paymentMethodPanel.IsVisible = false;
            }

            UpdatePayButtonVisibility();
        }

        // Вспомогательный метод для поиска элементов
        private T Find<T>(string name) where T : Control
        {
            var control = this.FindControl<T>(name);
            if (control == null)
                throw new InvalidOperationException($"Элемент управления '{name}' не найден");
            return control;
        }
    }
}