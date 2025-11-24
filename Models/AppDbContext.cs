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
    // Таблицы
    public DbSet<User> Users => Set<User>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<GlobalShift> GlobalShifts => Set<GlobalShift>();
    public DbSet<WaiterShift> WaiterShifts => Set<WaiterShift>();
    public DbSet<CashReceipt> CashReceipts => Set<CashReceipt>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Получаем путь к проекту
        var projectRoot = GetProjectRootDirectory();
        var dbPath = Path.Combine(projectRoot, "Data", "cafe.db");

        // Создаем папку Data, если её нет
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
        // Конвертер для GlobalShift.EmployeeIds (List<int> → string)
        modelBuilder.Entity<GlobalShift>()
            .Property(s => s.EmployeeIds)
            .HasConversion(
                v => string.Join(',', v),
                v => string.IsNullOrEmpty(v)
                    ? new List<int>()
                    : v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(int.Parse)
                        .ToList()
            );

        // Убрали конвертер для WaiterShiftIds, так как теперь используем навигационное свойство

        // Настройка отношений для WaiterShift
        modelBuilder.Entity<WaiterShift>()
            .HasOne(ws => ws.Waiter)
            .WithMany()
            .HasForeignKey(ws => ws.WaiterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WaiterShift>()
            .HasOne(ws => ws.GlobalShift)
            .WithMany(gs => gs.WaiterShifts)
            .HasForeignKey(ws => ws.GlobalShiftId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<WaiterShift>()
            .HasMany(ws => ws.Orders)
            .WithOne(o => o.WaiterShift)
            .HasForeignKey(o => o.WaiterShiftId)
            .OnDelete(DeleteBehavior.Restrict);

        // Настройка отношений для Order
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Waiter)
            .WithMany()
            .HasForeignKey(o => o.WaiterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.WaiterShift)
            .WithMany(ws => ws.Orders)
            .HasForeignKey(o => o.WaiterShiftId)
            .OnDelete(DeleteBehavior.Restrict);

        // Настройка отношений для CashReceipt
        modelBuilder.Entity<CashReceipt>()
            .HasOne(cr => cr.Waiter)
            .WithMany()
            .HasForeignKey(cr => cr.WaiterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CashReceipt>()
            .HasOne(cr => cr.Order)
            .WithMany()
            .HasForeignKey(cr => cr.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        // Явное задание имен таблиц
        modelBuilder.Entity<User>().ToTable("Users");
        modelBuilder.Entity<Order>().ToTable("Orders");
        modelBuilder.Entity<GlobalShift>().ToTable("GlobalShifts");
        modelBuilder.Entity<WaiterShift>().ToTable("WaiterShifts");
        modelBuilder.Entity<CashReceipt>().ToTable("CashReceipts");

        // Настройка decimal precision для денежных полей
        modelBuilder.Entity<Order>()
            .Property(o => o.TotalAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<CashReceipt>()
            .Property(cr => cr.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<WaiterShift>()
            .Property(ws => ws.TotalRevenue)
            .HasPrecision(18, 2);

        modelBuilder.Entity<WaiterShift>()
            .Property(ws => ws.CashRevenue)
            .HasPrecision(18, 2);

        modelBuilder.Entity<WaiterShift>()
            .Property(ws => ws.CardRevenue)
            .HasPrecision(18, 2);
    }
}