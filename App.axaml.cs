using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using MyApp.Views;
using MyApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace MyApp;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var context = new AppDbContext();
        context.Database.EnsureCreated();

        if (!context.Users.Any())
        {
            // Администратор
            context.Users.Add(new User
            {
                Username = "admin",
                Password = "admin123", 
                Role = "Admin",
                FullName = "Администратор"
            });

            // Повар
            context.Users.Add(new User
            {
                Username = "cook1",
                Password = "cook123",
                Role = "Cook", 
                FullName = "Повар Иванов"
            });

            // Официант
            context.Users.Add(new User
            {
                Username = "waiter1",
                Password = "waiter123",
                Role = "Waiter",
                FullName = "Официант Петров"
            });

            // Еще один повар
            context.Users.Add(new User
            {
                Username = "cook2", 
                Password = "cook456",
                Role = "Cook",
                FullName = "Повар Сидоров"
            });

            context.SaveChanges();
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new LoginWindow();
        }
        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}