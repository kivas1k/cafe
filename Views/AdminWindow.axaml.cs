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
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        // Используем правильные настройки для сохранения файла
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Сохранить отчёт по заказам",
            SuggestedFileName = $"Orders_Report_{DateTime.Now:dd-MM-yyyy_HH-mm}.pdf",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("PDF Document")
                {
                    Patterns = new[] { "*.pdf" },
                    AppleUniformTypeIdentifiers = new[] { "com.adobe.pdf" },
                    MimeTypes = new[] { "application/pdf" }
                }
            }
        });

        if (file == null) return;

        using var db = new AppDbContext();
        var orders = await db.Orders.OrderBy(o => o.CreatedAt).ToListAsync();

        // Генерируем PDF
        QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);

                // Указываем шрифт с поддержкой русского языка
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(12));

                // Заголовок
                page.Header()
                    .AlignCenter()
                    .Text("ОТЧЁТ ПО ЗАКАЗАМ")
                    .FontSize(20)
                    .SemiBold();

                // Основное содержимое
                page.Content()
                    .PaddingVertical(20)
                    .Column(col =>
                    {
                        col.Item().Text($"Сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}");
                        col.Item().Text($"Всего заказов: {orders.Count}").FontSize(14);
                        col.Item().PaddingTop(15);

                        if (orders.Any())
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(40);   // №
                                    columns.ConstantColumn(60);   // Стол
                                    columns.ConstantColumn(70);   // Гостей
                                    columns.ConstantColumn(90);   // Сумма
                                    columns.RelativeColumn();     // Время
                                    columns.ConstantColumn(80);   // Статус
                                });

                                // Заголовки таблицы
                                table.Header(header =>
                                {
                                    header.Cell().Text("№").Bold();
                                    header.Cell().Text("Стол").Bold();
                                    header.Cell().Text("Гостей").Bold();
                                    header.Cell().Text("Сумма").Bold();
                                    header.Cell().Text("Время").Bold();
                                    header.Cell().Text("Статус").Bold();
                                });

                                // Данные
                                int i = 1;
                                foreach (var order in orders)
                                {
                                    table.Cell().Text(i++.ToString());
                                    table.Cell().Text(order.TableNumber.ToString());
                                    table.Cell().Text(order.CustomersCount.ToString());
                                    table.Cell().Text($"{order.TotalAmount:F2} ₽");
                                    table.Cell().Text(order.CreatedAt.ToString("dd.MM HH:mm"));
                                    table.Cell().Text(order.Status);
                                }
                            });

                            // Итоговая сумма
                            var totalAmount = orders.Sum(o => o.TotalAmount);
                            var paidAmount = orders.Where(o => o.Status == "Paid").Sum(o => o.TotalAmount);
                            
                            col.Item().PaddingTop(15);
                            col.Item().Text($"Общая сумма всех заказов: {totalAmount:F2} ₽").SemiBold();
                            col.Item().Text($"Сумма оплаченных заказов: {paidAmount:F2} ₽").SemiBold();
                        }
                        else
                        {
                            col.Item().Text("Нет данных о заказах").Italic();
                        }
                    });

                // Футер с номерами страниц
                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Страница ");
                        x.CurrentPageNumber();
                        x.Span(" из ");
                        x.TotalPages();
                    });
            });
        })
        .GeneratePdf(file.Path.LocalPath); // Используем LocalPath вместо AbsolutePath

        await ShowMessageAsync($"PDF отчёт успешно создан!\nПуть: {file.Path.LocalPath}");
    }
    catch (Exception ex)
    {
        await ShowMessageAsync($"Ошибка при создании PDF: {ex.Message}\n\nДетали: {ex.InnerException?.Message}");
    }
}
        private async void GenerateRevenueReportXlsx_Click(object? sender, RoutedEventArgs e)
{
    try
    {
        var saver = this.StorageProvider;
        var file = await saver.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Сохранить отчёт по выручке",
            SuggestedFileName = $"Выручка_{DateTime.Now:dd-MM-yyyy}.xlsx",
            DefaultExtension = "xlsx",
            FileTypeChoices = new[] { new FilePickerFileType("Excel") { Patterns = new[] { "*.xlsx" } } }
        });

        if (file == null) return;

        using var db = new AppDbContext();
        var paidOrders = await db.Orders
            .Where(o => o.Status == "Paid")
            .OrderBy(o => o.PaidAt ?? o.CreatedAt)
            .ToListAsync();

        var totalRevenue = paidOrders.Sum(o => o.TotalAmount);

        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var ws = workbook.Worksheets.Add("Выручка");

        ws.Cell(1, 1).Value = "ОТЧЁТ ПО ВЫРУЧКЕ";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 16;

        ws.Cell(2, 1).Value = $"Период: все заказы";
        ws.Cell(3, 1).Value = $"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}";
        ws.Cell(4, 1).Value = $"Всего оплаченных заказов: {paidOrders.Count}";
        ws.Cell(5, 1).Value = $"ИТОГОВАЯ ВЫРУЧКА: {totalRevenue:0.00} ₽";
        ws.Cell(5, 1).Style.Font.Bold = true;
        ws.Cell(5, 1).Style.Font.FontSize = 14;
        ws.Cell(5, 1).Style.Font.FontColor = ClosedXML.Excel.XLColor.Green;

        ws.Cell(7, 1).Value = "№";
        ws.Cell(7, 2).Value = "Стол";
        ws.Cell(7, 3).Value = "Гостей";
        ws.Cell(7, 4).Value = "Сумма";
        ws.Cell(7, 5).Value = "Оплачен";

        var row = 8;
        int num = 1;
        foreach (var o in paidOrders)
        {
            ws.Cell(row, 1).Value = num++;
            ws.Cell(row, 2).Value = o.TableNumber;
            ws.Cell(row, 3).Value = o.CustomersCount;
            ws.Cell(row, 4).Value = o.TotalAmount;
            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00 ₽";
            ws.Cell(row, 5).Value = o.PaidAt?.ToString("dd.MM.yyyy HH:mm") ?? "Неизвестно";
            row++;
        }

        ws.Columns().AdjustToContents();

        workbook.SaveAs(file.Path.LocalPath);

        await ShowMessageAsync($"Отчёт по выручке сохранён:\n{file.Path.LocalPath}");
    }
    catch (Exception ex)
    {
        await ShowMessageAsync("Ошибка создания XLSX: " + ex.Message);
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