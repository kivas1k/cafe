using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MyApp.Models;
using MyApp.Services;
using ClosedXML.Excel;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MyApp.Views

{
    public partial class AdminWindow : Window
    {
        private readonly User _currentUser;
        private readonly AvaloniaList<User> _employees = new();
        private readonly AvaloniaList<Shift> _shifts = new();
        private readonly AvaloniaList<Order> _orders = new();

        public AdminWindow(User user)
        {
            InitializeComponent();
            _currentUser = user;

            EmployeesListBox.ItemsSource = _employees;
            ShiftsListBox.ItemsSource = _shifts;
            OrdersListBox.ItemsSource = _orders;

            this.Loaded += async (s, e) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            await Task.Run(() =>
            {
                using var db = new AppDbContext();
                var users = db.Users.Where(u => !u.IsFired).ToList();
                var orders = db.Orders.ToList();
                var shifts = db.Shifts.ToList();

                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _employees.Clear();
                    _employees.AddRange(users);
                    _orders.Clear();
                    _orders.AddRange(orders);
                    _shifts.Clear();
                    _shifts.AddRange(shifts);
                });
            });
        }

        private async void AddEmployee_Click(object? sender, RoutedEventArgs e)
        {
            var employeeWindow = new EmployeeCardWindow(null);
            await employeeWindow.ShowDialog(this);
            await LoadDataAsync();
        }

        private async void ViewEmployee_Click(object? sender, RoutedEventArgs e)
        {
            if (EmployeesListBox.SelectedItem is User selectedUser)
            {
                var employeeWindow = new EmployeeCardWindow(selectedUser);
                await employeeWindow.ShowDialog(this);
                await LoadDataAsync();
            }
        }

        private async void FireEmployee_Click(object? sender, RoutedEventArgs e)
        {
            if (EmployeesListBox.SelectedItem is User selectedUser)
            {
                selectedUser.IsFired = true;
                using var db = new AppDbContext();
                db.Users.Update(selectedUser);
                await db.SaveChangesAsync();
                await LoadDataAsync();
                await MessageBox.Show(this, "Сотрудник уволен.");
            }
        }

        private async void CreateShift_Click(object? sender, RoutedEventArgs e)
        {
            var newShift = new Shift { Date = DateTime.Now.AddDays(1), EmployeeIds = new List<int>() };
            using var db = new AppDbContext();
            db.Shifts.Add(newShift);
            await db.SaveChangesAsync();
            await LoadDataAsync();
            await MessageBox.Show(this, "Смена создана.");
        }

        private async void AssignToShift_Click(object? sender, RoutedEventArgs e)
        {
            if (ShiftsListBox.SelectedItem is Shift selectedShift && EmployeesListBox.SelectedItem is User selectedUser)
            {
                using var db = new AppDbContext();
                var shift = await db.Shifts.Include(s => s.EmployeeIds).FirstOrDefaultAsync(s => s.Id == selectedShift.Id);
                if (shift == null) return;

                if (shift.EmployeeIds.Count >= 7)
                {
                    await MessageBox.Show(this, "Максимум 7 сотрудников в смене.");
                    return;
                }

                if (shift.EmployeeIds.Count < 4)
                {
                    await MessageBox.Show(this, "Минимум 4 сотрудника в смене.");
                }

                if (!shift.EmployeeIds.Contains(selectedUser.Id))
                {
                    shift.EmployeeIds.Add(selectedUser.Id);
                    db.Shifts.Update(shift);
                    await db.SaveChangesAsync();
                    await LoadDataAsync();
                    await MessageBox.Show(this, "Сотрудник назначен.");
                }
            }
        }

        private async void EditOrder_Click(object? sender, RoutedEventArgs e)
        {
            if (OrdersListBox.SelectedItem is Order selectedOrder && selectedOrder.Status != "Paid")
            {
                // Простое редактирование, например, изменить статус
                selectedOrder.Status = "Edited";
                using var db = new AppDbContext();
                db.Orders.Update(selectedOrder);
                await db.SaveChangesAsync();
                await LoadDataAsync();
                await MessageBox.Show(this, "Заказ отредактирован.");
            }
        }

        private async void GenerateOrdersReportPdf_Click(object? sender, RoutedEventArgs e)
        {
            var filePicker = new FilePickerSaveOptions { Title = "Сохранить PDF отчёт" };
            var result = await StorageProvider.SaveFilePickerAsync(filePicker);
            if (result == null) return;

            using var db = new AppDbContext();
            var orders = await db.Orders.ToListAsync();

            await using var stream = await result.OpenWriteAsync();
            using var writer = new PdfWriter(stream);
            using var pdf = new PdfDocument(writer);
            var document = new Document(pdf);
            document.Add(new Paragraph("Отчёт по заказам"));
            foreach (var order in orders)
            {
                document.Add(new Paragraph($"Заказ {order.Id}: {order.Items}, Статус: {order.Status}"));
            }
            document.Close();

            await MessageBox.Show(this, "PDF отчёт сохранён.");
        }

        private async void GenerateRevenueReportXlsx_Click(object? sender, RoutedEventArgs e)
        {
            var filePicker = new FilePickerSaveOptions { Title = "Сохранить XLSX отчёт" };
            var result = await StorageProvider.SaveFilePickerAsync(filePicker);
            if (result == null) return;

            using var db = new AppDbContext();
            var paidOrders = await db.Orders.Where(o => o.Status == "Paid").ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Выручка");
            worksheet.Cell(1, 1).Value = "ID заказа";
            worksheet.Cell(1, 2).Value = "Блюда";
            worksheet.Cell(1, 3).Value = "Статус";

            for (int i = 0; i < paidOrders.Count; i++)
            {
                worksheet.Cell(i + 2, 1).Value = paidOrders[i].Id;
                worksheet.Cell(i + 2, 2).Value = paidOrders[i].Items;
                worksheet.Cell(i + 2, 3).Value = paidOrders[i].Status;
            }

            await using var stream = await result.OpenWriteAsync();
            workbook.SaveAs(stream);

            await MessageBox.Show(this, "XLSX отчёт сохранён.");
        }
    }
}