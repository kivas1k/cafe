using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Diagnostics;

namespace MyApp.Models;

public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Shift> Shifts => Set<Shift>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Путь к корневой папке проекта
        var projectRoot = GetProjectRootDirectory();
        var dbPath = Path.Combine(projectRoot, "Data", "cafe.db");
        
        // Создаем папку Data, если она не существует
        var dataDir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dataDir) && !Directory.Exists(dataDir))
        {
            Directory.CreateDirectory(dataDir);
        }
        
        Debug.WriteLine($"Database path: {dbPath}");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }

    private string GetProjectRootDirectory()
    {
        // Получаем текущую директорию исполняемого файла
        var currentDir = Directory.GetCurrentDirectory();
        
        // Ищем корень проекта (где находится .csproj файл)
        var directory = new DirectoryInfo(currentDir);
        while (directory != null && !directory.GetFiles("*.csproj").Any())
        {
            directory = directory.Parent;
        }
        
        return directory?.FullName ?? currentDir;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
    }
}