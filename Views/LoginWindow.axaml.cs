using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.EntityFrameworkCore;
using MyApp.Models;
using MyApp.Services;
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
        var password = PasswordTextBox.Text; // Используем TextBox.Text

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            await MessageBox.Show(this, "Введите логин и пароль.");
            return;
        }

        try
        {
            using var db = new AppDbContext();
            db.Database.EnsureCreated();

            var user = await db.Users
                .FirstOrDefaultAsync(u => 
                    u.Username == username &&
                    u.Password == password &&
                    !u.IsFired);

            if (user == null)
            {
                await MessageBox.Show(this, "Неверный логин или пароль.");
                return;
            }

            Window mainWindow = user.Role switch
            {
                "Admin" => new AdminWindow(user),
                "Waiter" => new WaiterWindow(user),
                "Cook" => new CookWindow(user),
                _ => throw new NotImplementedException()
            };

            mainWindow.Show();
            Close();
        }
        catch (Exception ex)
        {
            await MessageBox.Show(this, $"Ошибка: {ex.Message}");
        }
    }
}