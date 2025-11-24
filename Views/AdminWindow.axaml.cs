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
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MyApp.Views
{
    public partial class AdminWindow : Window
    {
        private readonly User _currentUser;
        private readonly AvaloniaList<User> _employees = new();
        private readonly AvaloniaList<ShiftDisplayItem> _shiftDisplayItems = new();
        private readonly AvaloniaList<Order> _orders = new();
        private readonly AvaloniaList<TableAssignment> _tableAssignments = new();

        public AdminWindow(User user)
        {
            InitializeComponent();
            _currentUser = user;

            EmployeesListBox.ItemsSource = _employees;
            ShiftsListBox.ItemsSource = _shiftDisplayItems;
            OrdersListBox.ItemsSource = _orders;
            TableAssignmentsListBox.ItemsSource = _tableAssignments;

            TableAssignmentShiftsListBox.SelectionChanged += TableAssignmentShiftsListBox_SelectionChanged;

            this.Loaded += async (s, e) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                await Task.Run(async () =>
                {
                    using var db = new AppDbContext();
                    var users = await db.Users.Where(u => !u.IsFired).ToListAsync();
                    var orders = await db.Orders.ToListAsync();
                    var shifts = await db.GlobalShifts.ToListAsync();

                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _employees.Clear();
                        _employees.AddRange(users);
                        _orders.Clear();
                        _orders.AddRange(orders);
                        _shiftDisplayItems.Clear();
                        _shiftDisplayItems.AddRange(shifts.Select(s => new ShiftDisplayItem(s)));
                        
                        TableAssignmentShiftsListBox.ItemsSource = _shiftDisplayItems;
                    });
                });
            }
            catch (Exception ex)
            {
                await ShowMessageAsync($"Ошибка загрузки данных: {ex.Message}");
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
                await ShowMessageAsync("Выберите сотрудника для просмотра.");
            }
        }

        private async void FireEmployee_Click(object? sender, RoutedEventArgs e)
        {
            if (EmployeesListBox.SelectedItem is not User selectedUser)
            {
                await ShowMessageAsync("Выберите сотрудника для увольнения.");
                return;
            }
            
            if (selectedUser.Id == _currentUser.Id)
            {
                await ShowMessageAsync("Вы не можете уволить самого себя.");
                return;
            }
            
            if (selectedUser.Role == "Admin")
            {
                await ShowMessageAsync("Нельзя уволить администратора. Сначала смените ему роль на Waiter или Cook.");
                return;
            }
            
            bool confirm1 = await ShowConfirmationDialogAsync(
                $"Вы действительно хотите уволить сотрудника «{selectedUser.FullName}» ({selectedUser.Role})?",
                "Подтверждение увольнения - Шаг 1/2");

            if (!confirm1) return;

            bool confirm2 = await ShowConfirmationDialogAsync(
                $"🔴 ВНИМАНИЕ: ЭТО ДЕЙСТВИЕ НЕОБРАТИМО! 🔴\n\n" +
                $"Вы увольняете: {selectedUser.FullName}\n" +
                $"Должность: {selectedUser.Role}\n\n" +
                $"Это действие нельзя отменить. Продолжить?",
                "ФИНАЛЬНОЕ ПОДТВЕРЖДЕНИЕ УВОЛЬНЕНИЯ");

            if (!confirm2) return;

            try
            {
                selectedUser.IsFired = true;
                using var db = new AppDbContext();
                db.Users.Update(selectedUser);
                await db.SaveChangesAsync();
                await LoadDataAsync();
                await ShowMessageAsync($"Сотрудник {selectedUser.FullName} успешно уволен.");
            }
            catch (Exception ex)
            {
                await ShowMessageAsync($"Ошибка при увольнении: {ex.Message}");
            }
        }

        private async void CreateShiftsForWeek_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                using var db = new AppDbContext();
                var createdShifts = new List<string>();

                var today = DateTime.Today;

                for (int i = 0; i < 5; i++)
                {
                    var shiftDate = today.AddDays(i);
                    string shiftName = $"Смена на {shiftDate:dd.MM.yyyy}";
                    
                    bool shiftExists = await db.GlobalShifts.AnyAsync(s => s.Name == shiftName);
                    if (!shiftExists)
                    {
                        var newShift = new GlobalShift 
                        { 
                            Name = shiftName,
                            Date = shiftDate, 
                            EmployeeIds = new List<int>(),
                            IsActive = true
                        };
                        
                        db.GlobalShifts.Add(newShift);
                        createdShifts.Add(shiftName);
                    }
                }

                if (createdShifts.Count > 0)
                {
                    await db.SaveChangesAsync();
                    await LoadDataAsync();
                    await ShowMessageAsync($"Успешно создано {createdShifts.Count} смен:\n" + string.Join("\n", createdShifts));
                }
                else
                {
                    await ShowMessageAsync("Все смены на ближайшие 5 дней уже созданы.");
                }
            }
            catch (Exception ex)
            {
                await ShowMessageAsync($"Ошибка создания смен: {ex.Message}");
            }
        }

        private async void DeleteShift_Click(object? sender, RoutedEventArgs e)
        {
            if (ShiftsListBox.SelectedItem is not ShiftDisplayItem selectedShiftItem)
            {
                await ShowMessageAsync("Выберите смену для удаления.");
                return;
            }

            var selectedShift = selectedShiftItem.Shift;

            bool confirm = await ShowConfirmationDialogAsync(
                $"Вы действительно хотите удалить смену «{selectedShift.Name}»?\n\n" +
                $"Дата: {selectedShift.Date:dd.MM.yyyy}\n" +
                $"Сотрудников в смене: {selectedShift.EmployeeIds.Count}\n\n" +
                $"Это действие нельзя отменить.",
                "Подтверждение удаления смены");

            if (!confirm) return;

            try
            {
                using var db = new AppDbContext();
                db.GlobalShifts.Remove(selectedShift);
                await db.SaveChangesAsync();
                await LoadDataAsync();
                await ShowMessageAsync($"Смена «{selectedShift.Name}» успешно удалена.");
            }
            catch (Exception ex)
            {
                await ShowMessageAsync($"Ошибка при удалении смены: {ex.Message}");
            }
        }

        private async void AssignToShift_Click(object? sender, RoutedEventArgs e)
        {
            if (ShiftsListBox.SelectedItem is not ShiftDisplayItem selectedShiftItem)
            {
                await ShowMessageAsync("Выберите смену для назначения.");
                return;
            }

            if (EmployeesListBox.SelectedItem is not User selectedUser)
            {
                await ShowMessageAsync("Выберите сотрудника для назначения.");
                return;
            }

            try
            {
                using var db = new AppDbContext();
                var shift = await db.GlobalShifts.FirstOrDefaultAsync(s => s.Id == selectedShiftItem.Id);
                if (shift == null) return;
                
                if (shift.EmployeeIds.Contains(selectedUser.Id))
                {
                    await ShowMessageAsync("Сотрудник уже назначен на эту смену.");
                    return;
                }

                if (shift.EmployeeIds.Count >= 7)
                {
                    await ShowMessageAsync("Максимум 7 сотрудников в смене.");
                    return;
                }
                
                shift.EmployeeIds.Add(selectedUser.Id);
                db.GlobalShifts.Update(shift);
                await db.SaveChangesAsync();
                await LoadDataAsync();
        
                string message = $"Сотрудник {selectedUser.FullName} назначен на смену «{shift.Name}».";
                if (shift.EmployeeIds.Count < 4)
                {
                    message += $"\n\n⚠️ Внимание: в смене всего {shift.EmployeeIds.Count} сотрудников. Минимум 4 сотрудника рекомендуется для нормальной работы.";
                }
        
                await ShowMessageAsync(message);
            }
            catch (Exception ex)
            {
                await ShowMessageAsync($"Ошибка назначения на смену: {ex.Message}");
            }
        }

        private async void RemoveFromShift_Click(object? sender, RoutedEventArgs e)
        {
            if (ShiftsListBox.SelectedItem is not ShiftDisplayItem selectedShiftItem)
            {
                await ShowMessageAsync("Выберите смену для удаления сотрудника.");
                return;
            }

            if (EmployeesListBox.SelectedItem is not User selectedUser)
            {
                await ShowMessageAsync("Выберите сотрудника для удаления из смены.");
                return;
            }

            try
            {
                using var db = new AppDbContext();
                var shift = await db.GlobalShifts.FirstOrDefaultAsync(s => s.Id == selectedShiftItem.Id);
                if (shift == null) return;
                
                if (!shift.EmployeeIds.Contains(selectedUser.Id))
                {
                    await ShowMessageAsync($"Сотрудник {selectedUser.FullName} не назначен на смену «{shift.Name}».");
                    return;
                }
                
                bool confirm = await ShowConfirmationDialogAsync(
                    $"Вы действительно хотите удалить сотрудника {selectedUser.FullName} из смены «{shift.Name}»?\n\n" +
                    $"Дата смены: {shift.Date:dd.MM.yyyy}\n" +
                    $"После удаления в смене останется {shift.EmployeeIds.Count - 1} сотрудников.",
                    "Подтверждение удаления из смены");

                if (!confirm) return;
                
                shift.EmployeeIds.Remove(selectedUser.Id);
                db.GlobalShifts.Update(shift);
                await db.SaveChangesAsync();
                await LoadDataAsync();
                
                await ShowMessageAsync($"Сотрудник {selectedUser.FullName} успешно удалён из смены «{shift.Name}».");
            }
            catch (Exception ex)
            {
                await ShowMessageAsync($"Ошибка при удалении сотрудника из смены: {ex.Message}");
            }
        }

        private async void ViewShiftEmployees_Click(object? sender, RoutedEventArgs e)
        {
            if (ShiftsListBox.SelectedItem is not ShiftDisplayItem selectedShiftItem)
            {
                await ShowMessageAsync("Выберите смену для просмотра сотрудников.");
                return;
            }

            var selectedShift = selectedShiftItem.Shift;

            try
            {
                using var db = new AppDbContext();
                
                var allEmployees = await db.Users.Where(u => !u.IsFired).ToListAsync();
                var shiftEmployees = allEmployees
                    .Where(emp => selectedShift.EmployeeIds.Contains(emp.Id))
                    .ToList();

                if (shiftEmployees.Count == 0)
                {
                    await ShowMessageAsync($"В смене «{selectedShift.Name}» нет назначенных сотрудников.");
                    return;
                }

                var employeeList = string.Join("\n", shiftEmployees
                    .Select((emp, index) => $"{index + 1}. {emp.FullName} ({emp.Role})"));

                await ShowMessageAsync(
                    $"Сотрудники в смене «{selectedShift.Name}»:\n\n" +
                    $"{employeeList}\n\n" +
                    $"Всего сотрудников: {shiftEmployees.Count}/7\n" +
                    $"{(shiftEmployees.Count < 4 ? "⚠️ Не хватает сотрудников! Минимум 4." : "✅ Штат укомплектован")}");
            }
            catch (Exception ex)
            {
                await ShowMessageAsync($"Ошибка при получении списка сотрудников: {ex.Message}");
            }
        }

        private async void EditOrder_Click(object? sender, RoutedEventArgs e)
        {
            if (OrdersListBox.SelectedItem is not Order order)
            {
                await ShowMessageAsync("Выберите заказ для редактирования");
                return;
            }

            if (order.Status == "Paid")
            {
                await ShowMessageAsync("Оплаченные заказы нельзя редактировать");
                return;
            }

            var editWindow = new OrderSimpleEditWindow(order);
            await editWindow.ShowDialog(this);

            if (editWindow.WasSaved)
            {
                using var db = new AppDbContext();
                db.Orders.Update(order);
                await db.SaveChangesAsync();

                await LoadDataAsync();
                await ShowMessageAsync("Заказ успешно изменён!");
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

                await ShowMessageAsync("PDF-отчёт по заказам успешно сохранён.");
            }
            catch (Exception ex)
            {
                await ShowMessageAsync($"Ошибка создания PDF-отчёта: {ex.Message}");
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

                await ShowMessageAsync("XLSX-отчёт по выручке успешно сохранён.");
            }
            catch (Exception ex)
            {
                await ShowMessageAsync($"Ошибка создания XLSX-отчёта: {ex.Message}");
            }
        }

        // === МЕТОДЫ ДЛЯ НАЗНАЧЕНИЯ СТОЛИКОВ ===

        private async void AssignTable_Click(object? sender, RoutedEventArgs e)
        {
            // ДЛЯ ОТЛАДКИ - проверяем значения
            Console.WriteLine($"TableNumberBox Value: {TableNumberBox.Value}");
            
            if (TableAssignmentShiftsListBox.SelectedItem is not ShiftDisplayItem selectedShift)
            {
                await ShowMessageAsync("Выберите смену для назначения.");
                return;
            }

            if (TableAssignmentWaitersListBox.SelectedItem is not User selectedWaiter)
            {
                await ShowMessageAsync("Выберите официанта для назначения.");
                return;
            }

            if (selectedWaiter.Role != "Waiter")
            {
                await ShowMessageAsync("Можно назначать только официантов на столики.");
                return;
            }

            var tableNumber = (int)TableNumberBox.Value;

            try
            {
                using var db = new AppDbContext();
                
                var existingAssignment = await db.TableAssignments
                    .FirstOrDefaultAsync(ta => 
                        ta.GlobalShiftId == selectedShift.Id && 
                        ta.TableNumber == tableNumber &&
                        ta.IsActive);

                if (existingAssignment != null)
                {
                    await ShowMessageAsync($"Столик {tableNumber} уже назначен официанту {existingAssignment.WaiterName}.");
                    return;
                }

                var assignment = new TableAssignment
                {
                    TableNumber = tableNumber,
                    WaiterId = selectedWaiter.Id,
                    GlobalShiftId = selectedShift.Id,
                    AssignedAt = DateTime.Now,
                    IsActive = true
                };

                db.TableAssignments.Add(assignment);
                await db.SaveChangesAsync();

                await LoadTableAssignmentsAsync(selectedShift.Id);
                await ShowMessageAsync($"Столик {tableNumber} назначен официанту {selectedWaiter.FullName}.");
            }
            catch (Exception ex)
            {
                await ShowMessageAsync($"Ошибка назначения столика: {ex.Message}");
            }
        }

        private async void UnassignTable_Click(object? sender, RoutedEventArgs e)
        {
            if (TableAssignmentsListBox.SelectedItem is not TableAssignment selectedAssignment)
            {
                await ShowMessageAsync("Выберите назначение для снятия.");
                return;
            }

            try
            {
                using var db = new AppDbContext();
                selectedAssignment.IsActive = false;
                db.TableAssignments.Update(selectedAssignment);
                await db.SaveChangesAsync();

                await LoadTableAssignmentsAsync(selectedAssignment.GlobalShiftId);
                await ShowMessageAsync($"Назначение столика {selectedAssignment.TableNumber} снято.");
            }
            catch (Exception ex)
            {
                await ShowMessageAsync($"Ошибка снятия назначения: {ex.Message}");
            }
        }

        private async void TableAssignmentShiftsListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (TableAssignmentShiftsListBox.SelectedItem is ShiftDisplayItem selectedShift)
            {
                await LoadWaitersForShiftAsync(selectedShift.Id);
                await LoadTableAssignmentsAsync(selectedShift.Id);
                
                TableAssignmentInfoText.Text = $"Назначения для смены: {selectedShift.Name}";
            }
            else
            {
                TableAssignmentWaitersListBox.ItemsSource = null;
                _tableAssignments.Clear();
                TableAssignmentInfoText.Text = "Выберите смену и официанта для назначения столиков.";
            }
        }

        private async Task LoadWaitersForShiftAsync(int shiftId)
        {
            try
            {
                using var db = new AppDbContext();
        
                var shift = await db.GlobalShifts
                    .FirstOrDefaultAsync(gs => gs.Id == shiftId);

                if (shift != null)
                {
                    var allUsers = await db.Users
                        .Where(u => !u.IsFired)
                        .ToListAsync();

                    var waiters = allUsers
                        .Where(u => u.Role == "Waiter" && shift.EmployeeIds.Contains(u.Id))
                        .ToList();

                    TableAssignmentWaitersListBox.ItemsSource = waiters;
            
                    if (!waiters.Any())
                    {
                        TableAssignmentInfoText.Text = $"В смене «{shift.Name}» нет назначенных официантов.";
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowMessageAsync($"Ошибка загрузки официантов: {ex.Message}");
            }
        }

        private async Task LoadTableAssignmentsAsync(int shiftId)
        {
            try
            {
                using var db = new AppDbContext();
                var assignments = await db.TableAssignments
                    .Include(ta => ta.Waiter)
                    .Where(ta => ta.GlobalShiftId == shiftId && ta.IsActive)
                    .OrderBy(ta => ta.TableNumber)
                    .ToListAsync();

                _tableAssignments.Clear();
                _tableAssignments.AddRange(assignments);
            }
            catch (Exception ex)
            {
                await ShowMessageAsync($"Ошибка загрузки назначений: {ex.Message}");
            }
        }

        private async Task ShowMessageAsync(string message)
        {
            await MessageBox.Show(this, message);
        }

        private async Task<bool> ShowConfirmationDialogAsync(string message, string title)
        {
            var dialog = new ConfirmationDialogWindow(message, title);
            await dialog.ShowDialog(this);
            return dialog.Result;
        }
    }
}