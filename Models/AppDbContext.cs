using System;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace MyApp.Models;

public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Shift> Shifts => Set<Shift>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dbPath = Path.Combine(AppContext.BaseDirectory, "cafe.db");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, Username = "admin", Password = "admin123", Role = "Admin", FullName = "Администратор" },
            new User { Id = 2, Username = "waiter", Password = "123", Role = "Waiter", FullName = "Иван Иванов" },
            new User { Id = 3, Username = "cook", Password = "123", Role = "Cook", FullName = "Пётр Петров" }
        );
    }
}