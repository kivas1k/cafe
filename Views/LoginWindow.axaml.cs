using Avalonia.Controls;
using Avalonia.Interactivity;
using MyApp.Models;
using MyApp.Services;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;

namespace MyApp.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
    }

    private async void LoginButton_Click(object? sender, RoutedEventArgs e)
    {
        var username = UsernameTextBox.Text?.Trim();
        var password = PasswordTextBox.Text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            await MessageBox.Show(this, "Введите логин и пароль.");
            return;
        }

        try
        {
            using var db = new AppDbContext();
            
            // ← ЭТО ГЛАВНОЕ: создаём базу, если её нет
            db.Database.EnsureCreated();

            var user = await db.Users
                .FirstOrDefaultAsync(u => u.Username == username && 
                                         u.Password == password && 
                                         !u.IsFired);

            if (user == null)
            {
                await MessageBox.Show(this, "Неверный логин или пароль.");
                return;
            }

            // Открываем нужное окно
            Window mainWindow = user.Role switch
            {
                "Admin" => new AdminWindow(user),
                "Waiter" => new WaiterWindow(user),
                "Cook" => new CookWindow(user),
                _ => throw new NotImplementedException()
            };

            mainWindow.Show();
            this.Close();
        }
        catch (Exception ex)
        {
            await MessageBox.Show(this, $"Ошибка: {ex.Message}");
        }
    }
}