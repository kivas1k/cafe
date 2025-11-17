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
            try
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
            catch (Exception ex)
            {
                await MessageBox.Show(this, $"Ошибка загрузки данных: {ex.Message}");
            }
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
            else
            {
                await MessageBox.Show(this, "Выберите сотрудника для просмотра.");
            }
        }

        private async void FireEmployee_Click(object? sender, RoutedEventArgs e)
        {
            if (EmployeesListBox.SelectedItem is not User selectedUser)
            {
                await MessageBox.Show(this, "Выберите сотрудника для увольнения.");
                return;
            }
            
            if (selectedUser.Id == _currentUser.Id)
            {
                await MessageBox.Show(this, "Вы не можете уволить самого себя.");
                return;
            }
            
            if (selectedUser.Role == "Admin")
            {
                await MessageBox.Show(this, "Нельзя уволить администратора. Сначала смените ему роль на Waiter или Cook.");
                return;
            }
            
            bool confirm = await MessageBox.Show(this,
                $"Уволить сотрудника «{selectedUser.FullName}» ({selectedUser.Role})?",
                "Подтверждение увольнения",
                true);

            if (!confirm) return;

            try
            {
                selectedUser.IsFired = true;
                using var db = new AppDbContext();
                db.Users.Update(selectedUser);
                await db.SaveChangesAsync();
                await LoadDataAsync();
                await MessageBox.Show(this, $"Сотрудник {selectedUser.FullName} успешно уволен.");
            }
            catch (Exception ex)
            {
                await MessageBox.Show(this, $"Ошибка при увольнении: {ex.Message}");
            }
        }

        private async void CreateShift_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var newShift = new Shift { Date = DateTime.Now.AddDays(1), EmployeeIds = new List<int>() };
                using var db = new AppDbContext();
                db.Shifts.Add(newShift);
                await db.SaveChangesAsync();
                await LoadDataAsync();
                await MessageBox.Show(this, "Смена создана.");
            }
            catch (Exception ex)
            {
                await MessageBox.Show(this, $"Ошибка создания смены: {ex.Message}");
            }
        }

        private async void AssignToShift_Click(object? sender, RoutedEventArgs e)
        {
            if (ShiftsListBox.SelectedItem is not Shift selectedShift)
            {
                await MessageBox.Show(this, "Выберите смену для назначения.");
                return;
            }

            if (EmployeesListBox.SelectedItem is not User selectedUser)
            {
                await MessageBox.Show(this, "Выберите сотрудника для назначения.");
                return;
            }

            try
            {
                using var db = new AppDbContext();
                var shift = await db.Shifts.FirstOrDefaultAsync(s => s.Id == selectedShift.Id);
                if (shift == null) return;

                if (shift.EmployeeIds.Count >= 7)
                {
                    await MessageBox.Show(this, "Максимум 7 сотрудников в смене.");
                    return;
                }

                if (shift.EmployeeIds.Count < 4)
                {
                    await MessageBox.Show(this, "Внимание: в смене меньше 4 сотрудников. Минимум 4 сотрудника рекомендуется для нормальной работы.");
                }

                if (!shift.EmployeeIds.Contains(selectedUser.Id))
                {
                    shift.EmployeeIds.Add(selectedUser.Id);
                    db.Shifts.Update(shift);
                    await db.SaveChangesAsync();
                    await LoadDataAsync();
                    await MessageBox.Show(this, "Сотрудник назначен на смену.");
                }
                else
                {
                    await MessageBox.Show(this, "Сотрудник уже назначен на эту смену.");
                }
            }
            catch (Exception ex)
            {
                await MessageBox.Show(this, $"Ошибка назначения на смену: {ex.Message}");
            }
        }

        private async void EditOrder_Click(object? sender, RoutedEventArgs e)
        {
            if (OrdersListBox.SelectedItem is not Order selectedOrder)
            {
                await MessageBox.Show(this, "Выберите заказ для редактирования.");
                return;
            }

            if (selectedOrder.Status == "Paid")
            {
                await MessageBox.Show(this, "Нельзя редактировать оплаченный заказ.");
                return;
            }

            try
            {
                // Простое редактирование, например, изменить статус
                selectedOrder.Status = "Edited";
                using var db = new AppDbContext();
                db.Orders.Update(selectedOrder);
                await db.SaveChangesAsync();
                await LoadDataAsync();
                await MessageBox.Show(this, "Заказ отредактирован.");
            }
            catch (Exception ex)
            {
                await MessageBox.Show(this, $"Ошибка редактирования заказа: {ex.Message}");
            }
        }

        private async void GenerateOrdersReportPdf_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var filePicker = new FilePickerSaveOptions 
                { 
                    Title = "Сохранить PDF отчёт",
                    SuggestedFileName = $"orders_report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                };
                
                var result = await StorageProvider.SaveFilePickerAsync(filePicker);
                if (result == null) return;

                using var db = new AppDbContext();
                var orders = await db.Orders.ToListAsync();

                await using var stream = await result.OpenWriteAsync();
                using var writer = new PdfWriter(stream);
                using var pdf = new PdfDocument(writer);
                var document = new Document(pdf);
                
                document.Add(new Paragraph("ОТЧЁТ ПО ВСЕМ ЗАКАЗАМ")
                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                    .SetBold()
                    .SetFontSize(16));

                document.Add(new Paragraph($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}"));
                document.Add(new Paragraph($"Всего заказов: {orders.Count}"));
                document.Add(new Paragraph(" "));
                
                Table table = new Table(4, true);
                table.AddHeaderCell("ID заказа");
                table.AddHeaderCell("Номер стола");
                table.AddHeaderCell("Блюда");
                table.AddHeaderCell("Статус");

                foreach (var order in orders)
                {
                    table.AddCell(order.Id.ToString());
                    table.AddCell(order.TableNumber.ToString());
                    table.AddCell(order.Items ?? "Нет данных");
                    table.AddCell(order.Status ?? "Не указан");
                }

                document.Add(table);
                document.Close();

                await MessageBox.Show(this, "PDF-отчёт по заказам успешно сохранён.");
            }
            catch (Exception ex)
            {
                await MessageBox.Show(this, $"Ошибка создания PDF-отчёта: {ex.Message}");
            }
        }

        private async void GenerateRevenueReportXlsx_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var filePicker = new FilePickerSaveOptions 
                { 
                    Title = "Сохранить XLSX отчёт",
                    SuggestedFileName = $"revenue_report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };
                
                var result = await StorageProvider.SaveFilePickerAsync(filePicker);
                if (result == null) return;

                using var db = new AppDbContext();
                var paidOrders = await db.Orders.Where(o => o.Status == "Paid").ToListAsync();

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Выручка");

                worksheet.Cell(1, 1).Value = "Отчёт по выручке";
                worksheet.Range(1, 1, 1, 4).Merge().Style.Font.Bold = true;
                worksheet.Cell(2, 1).Value = $"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}";
                worksheet.Cell(3, 1).Value = $"Всего оплаченных заказов: {paidOrders.Count}";
                
                worksheet.Cell(5, 1).Value = "ID заказа";
                worksheet.Cell(5, 2).Value = "Номер стола";
                worksheet.Cell(5, 3).Value = "Блюда";
                worksheet.Cell(5, 4).Value = "Способ оплаты";
                
                var headerRange = worksheet.Range(5, 1, 5, 4);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                
                for (int i = 0; i < paidOrders.Count; i++)
                {
                    worksheet.Cell(i + 6, 1).Value = paidOrders[i].Id;
                    worksheet.Cell(i + 6, 2).Value = paidOrders[i].TableNumber;
                    worksheet.Cell(i + 6, 3).Value = paidOrders[i].Items;
                    worksheet.Cell(i + 6, 4).Value = paidOrders[i].PaymentMethod ?? "Не указан";
                }
                
                worksheet.Columns().AdjustToContents();

                await using var stream = await result.OpenWriteAsync();
                workbook.SaveAs(stream);

                await MessageBox.Show(this, "XLSX-отчёт по выручке успешно сохранён.");
            }
            catch (Exception ex)
            {
                await MessageBox.Show(this, $"Ошибка создания XLSX-отчёта: {ex.Message}");
            }
        }
    }
}