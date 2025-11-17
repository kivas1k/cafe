using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MyApp.Models;
using MyApp.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyApp.Views;

public partial class EmployeeCardWindow : Window
{
    private readonly User _user = new();
    private string? _photoPath;
    private string? _contractPath;

    public List<string> Roles { get; } = new() { "Admin", "Waiter", "Cook" };
    public string? SelectedRole { get; set; }

    public EmployeeCardWindow(User? user = null)
    {
        InitializeComponent();
        DataContext = this;

        if (user != null) _user = user;

        FullNameTextBox.Text = _user.FullName;
        UsernameTextBox.Text = _user.Username;
        PasswordTextBox.Text = _user.Password;
        SelectedRole = _user.Role ?? "Waiter";

        PhotoDropBorder.AddHandler(DragDrop.DropEvent, PhotoDrop);
        PhotoDropBorder.AddHandler(DragDrop.DragOverEvent, DragOver);
        ContractDropBorder.AddHandler(DragDrop.DropEvent, ContractDrop);
        ContractDropBorder.AddHandler(DragDrop.DragOverEvent, DragOver);
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.Data.Contains(DataFormats.Files)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private async void PhotoDrop(object? sender, DragEventArgs e)
    {
        var file = e.Data.GetFiles()?.FirstOrDefault() as IStorageFile;
        if (file != null) await SaveFile(file, true);
    }

    private async void ContractDrop(object? sender, DragEventArgs e)
    {
        var file = e.Data.GetFiles()?.FirstOrDefault() as IStorageFile;
        if (file != null) await SaveFile(file, false);
    }

    private async void AddPhoto_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { AllowMultiple = false });
        if (files?.Count > 0) await SaveFile(files[0] as IStorageFile, true);
    }

    private async void AddContract_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { AllowMultiple = false });
        if (files?.Count > 0) await SaveFile(files[0] as IStorageFile, false);
    }

    private async Task SaveFile(IStorageFile? file, bool isPhoto)
    {
        if (file == null) return;
        var folder = Path.Combine(Directory.GetCurrentDirectory(), "Files");
        Directory.CreateDirectory(folder);
        var dest = Path.Combine(folder, file.Name);

        try
        {
            await using var src = await file.OpenReadAsync();
            await using var dst = File.Create(dest);
            await src.CopyToAsync(dst);
            if (isPhoto) _photoPath = dest;
            else _contractPath = dest;
        }
        catch { }
    }

    private async void Save_Click(object? sender, RoutedEventArgs e)
    {
        _user.FullName = FullNameTextBox.Text ?? "";
        _user.Username = UsernameTextBox.Text ?? "";
        _user.Password = PasswordTextBox.Text ?? "";
        _user.Role = SelectedRole ?? "Waiter";
        _user.PhotoPath = _photoPath;
        _user.ContractPath = _contractPath;

        using var db = new AppDbContext();
        if (_user.Id == 0) db.Users.Add(_user);
        else db.Users.Update(_user);
        await db.SaveChangesAsync();

        await MessageBox.Show(this, "Сохранено!");
        Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close();
}