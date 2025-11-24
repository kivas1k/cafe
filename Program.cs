using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using QuestPDF.Infrastructure;   

namespace MyApp;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // ПРАВИЛЬНО ДЛЯ ВЕРСИИ 2023.12.6
        QuestPDF.Settings.License = LicenseType.Community;

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}