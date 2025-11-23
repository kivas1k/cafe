// Models/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace MyApp.Models;

public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<CashReceipt> CashReceipts => Set<CashReceipt>(); // ДОБАВЛЕНО!

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var projectRoot = GetProjectRootDirectory();
        var dbPath = Path.Combine(projectRoot, "Data", "cafe.db");

        var dataDir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dataDir) && !Directory.Exists(dataDir))
        {
            Directory.CreateDirectory(dataDir);
        }

        Debug.WriteLine($"[DB] Путь к базе данных: {dbPath}");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }

    private string GetProjectRootDirectory()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var directory = new DirectoryInfo(currentDir);

        while (directory != null && !directory.GetFiles("*.csproj").Any())
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? currentDir;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Конвертер для List<int> → строка в БД
        modelBuilder.Entity<Shift>()
            .Property(s => s.EmployeeIds)
            .HasConversion(
                v => string.Join(',', v),
                v => string.IsNullOrEmpty(v)
                    ? new List<int>()
                    : v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(int.Parse)
                        .ToList()
            );

        // Дополнительно: можно задать имена таблиц явно (по желанию)
        modelBuilder.Entity<User>().ToTable("Users");
        modelBuilder.Entity<Order>().ToTable("Orders");
        modelBuilder.Entity<Shift>().ToTable("Shifts");
        modelBuilder.Entity<CashReceipt>().ToTable("CashReceipts");
    }
}