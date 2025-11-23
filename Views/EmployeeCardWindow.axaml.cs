using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using MyApp.Models;
using MyApp.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Primitives;

namespace MyApp.Views;

public partial class EmployeeCardWindow : Window
{
    private readonly User _user;
    private string? _photoPath;
    private string? _contractPath;
    private bool _isEditMode;
    private readonly string _filesDirectory;

    public EmployeeCardWindow(User? user = null)
    {
        InitializeComponent();
    
        _user = user ?? new User();
        _isEditMode = user != null;
        
        // Определяем путь к папке Files
        _filesDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Files");
        Directory.CreateDirectory(_filesDirectory);
        
        InitializeUI();
    
        // Настройка обработчиков drag & drop
        PhotoDropBorder.AddHandler(DragDrop.DropEvent, PhotoDrop);
        PhotoDropBorder.AddHandler(DragDrop.DragOverEvent, DragOver);
        ContractDropBorder.AddHandler(DragDrop.DropEvent, ContractDrop);
        ContractDropBorder.AddHandler(DragDrop.DragOverEvent, DragOver);
    }

    private void InitializeUI()
    {
        WindowTitleText.Text = _isEditMode ? "Редактирование сотрудника" : "Новый сотрудник";

        // Заполнение полей данными
        if (_isEditMode)
        {
            FullNameTextBox.Text = _user.FullName ?? "";
            UsernameTextBox.Text = _user.Username ?? "";
            PasswordTextBox.Text = _user.Password ?? "";
            _photoPath = GetFullFilePath(_user.PhotoPath);
            _contractPath = GetFullFilePath(_user.ContractPath);
        }
        
        RoleComboBox.ItemsSource = new List<string> { "Admin", "Waiter", "Cook" };
        RoleComboBox.SelectedItem = _user.Role ?? "Waiter";
        
        UpdateFileDisplays();
    }

    private string? GetFullFilePath(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return null;
        
        if (Path.IsPathRooted(filePath))
            return filePath;
        
        return Path.Combine(_filesDirectory, Path.GetFileName(filePath));
    }

    private string? GetRelativeFilePath(string? fullPath)
    {
        if (string.IsNullOrEmpty(fullPath))
            return null;
        
        return Path.GetFileName(fullPath);
    }

    private void UpdateFileDisplays()
    {
 
        if (!string.IsNullOrEmpty(_photoPath) && File.Exists(_photoPath))
        {
            PhotoTextBlock.Text = "Фото\nзагружено";
            PhotoDropBorder.Background = Brushes.LightGreen;
            PhotoDropBorder.BorderBrush = Brushes.Green;
            RemovePhotoButton.IsVisible = true;
            ViewPhotoButton.IsVisible = true;
        }
        else
        {
            PhotoTextBlock.Text = "Перетащите\nфото сюда";
            PhotoDropBorder.Background = Brushes.LightGray;
            PhotoDropBorder.BorderBrush = Brushes.Gray;
            RemovePhotoButton.IsVisible = false;
            ViewPhotoButton.IsVisible = false;
            _photoPath = null;
        }
        
        if (!string.IsNullOrEmpty(_contractPath) && File.Exists(_contractPath))
        {
            ContractTextBlock.Text = "Договор\nзагружен";
            ContractDropBorder.Background = Brushes.LightGreen;
            ContractDropBorder.BorderBrush = Brushes.Green;
            RemoveContractButton.IsVisible = true;
            DownloadContractButton.IsVisible = true;
        }
        else
        {
            ContractTextBlock.Text = "Перетащите\nдоговор сюда";
            ContractDropBorder.Background = Brushes.LightGray;
            ContractDropBorder.BorderBrush = Brushes.Gray;
            RemoveContractButton.IsVisible = false;
            DownloadContractButton.IsVisible = false;
            _contractPath = null;
        }
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.Data.GetFiles()?.Any() == true ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void PhotoDrop(object? sender, DragEventArgs e)
    {
        var files = e.Data.GetFiles()?.OfType<IStorageFile>().ToList();
        if (files?.Count > 0)
        {
            await SaveFile(files[0], true);
        }
    }

    private async void ContractDrop(object? sender, DragEventArgs e)
    {
        var files = e.Data.GetFiles()?.OfType<IStorageFile>().ToList();
        if (files?.Count > 0)
        {
            await SaveFile(files[0], false);
        }
    }

    private async void AddPhoto_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions 
        { 
            AllowMultiple = false,
            Title = "Выберите фото сотрудника",
            FileTypeFilter = new[] 
            {
                new FilePickerFileType("Изображения") { Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp" } }
            }
        });
        
        if (files?.Count > 0)
        {
            await SaveFile(files[0], true);
        }
    }

    private async void AddContract_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions 
        { 
            AllowMultiple = false,
            Title = "Выберите трудовой договор",
            FileTypeFilter = new[] 
            {
                new FilePickerFileType("Документы") { Patterns = new[] { "*.pdf", "*.doc", "*.docx" } }
            }
        });
        
        if (files?.Count > 0)
        {
            await SaveFile(files[0], false);
        }
    }

    private void RemovePhoto_Click(object? sender, RoutedEventArgs e)
    {
        _photoPath = null;
        UpdateFileDisplays();
    }

    private void RemoveContract_Click(object? sender, RoutedEventArgs e)
    {
        _contractPath = null;
        UpdateFileDisplays();
    }
    
    private async void ViewPhoto_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_photoPath) || !File.Exists(_photoPath))
        {
            await MessageBox.Show(this, "Фото не найдено!", "Ошибка");
            return;
        }

        try
        {
            var imageViewer = new Window
            {
                Title = $"Фото сотрудника: {_user.FullName}",
                Width = 600,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var image = new Image
            {
                Source = new Avalonia.Media.Imaging.Bitmap(_photoPath),
                Stretch = Stretch.Uniform,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            var scrollViewer = new ScrollViewer
            {
                Content = image,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            imageViewer.Content = scrollViewer;
            await imageViewer.ShowDialog(this);
        }
        catch (Exception ex)
        {
            await MessageBox.Show(this, $"Ошибка при открытии фото: {ex.Message}", "Ошибка");
        }
    }
    
    private async void DownloadContract_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_contractPath) || !File.Exists(_contractPath))
        {
            await MessageBox.Show(this, "Договор не найдено!", "Ошибка");
            return;
        }

        try
        {
            var fileExtension = Path.GetExtension(_contractPath);
            var defaultFileName = $"Договор_{_user.FullName}_{DateTime.Now:yyyyMMdd}{fileExtension}";
            
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                defaultFileName = defaultFileName.Replace(invalidChar, '_');
            }

            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Сохранить трудовой договор",
                SuggestedFileName = defaultFileName,
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Документы") 
                    { 
                        Patterns = new[] { "*.pdf", "*.doc", "*.docx" } 
                    }
                }
            });

            if (file != null)
            {
                File.Copy(_contractPath, file.Path.LocalPath, overwrite: true);
                await MessageBox.Show(this, "Договор успешно сохранен!", "Успех");
            }
        }
        catch (Exception ex)
        {
            await MessageBox.Show(this, $"Ошибка при сохранении договора: {ex.Message}", "Ошибка");
        }
    }

    private async Task SaveFile(IStorageFile? file, bool isPhoto)
    {
        if (file == null) return;
        
        try
        {
            var uniqueFileName = $"{Guid.NewGuid()}_{file.Name}";
            var dest = Path.Combine(_filesDirectory, uniqueFileName);

            await using var src = await file.OpenReadAsync();
            await using var dst = File.Create(dest);
            await src.CopyToAsync(dst);
            
            if (isPhoto)
                _photoPath = dest;
            else
                _contractPath = dest;
            
            UpdateFileDisplays();
            ErrorTextBlock.Text = "";
        }
        catch (Exception ex)
        {
            ErrorTextBlock.Text = $"Ошибка загрузки файла: {ex.Message}";
        }
    }

    private bool ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(FullNameTextBox.Text))
        {
            ErrorTextBlock.Text = "Введите полное имя сотрудника";
            return false;
        }

        if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
        {
            ErrorTextBlock.Text = "Введите логин сотрудника";
            return false;
        }

        if (string.IsNullOrWhiteSpace(PasswordTextBox.Text))
        {
            ErrorTextBlock.Text = "Введите пароль сотрудника";
            return false;
        }

        if (RoleComboBox.SelectedItem == null)
        {
            ErrorTextBlock.Text = "Выберите роль сотрудника";
            return false;
        }

        ErrorTextBlock.Text = "";
        return true;
    }

    private async void Save_Click(object? sender, RoutedEventArgs e)
    {
        if (!ValidateForm())
            return;

        try
        {
            _user.FullName = FullNameTextBox.Text?.Trim() ?? "";
            _user.Username = UsernameTextBox.Text?.Trim() ?? "";
            _user.Password = PasswordTextBox.Text ?? "";
            _user.Role = RoleComboBox.SelectedItem as string ?? "Waiter";
            
            _user.PhotoPath = GetRelativeFilePath(_photoPath);
            _user.ContractPath = GetRelativeFilePath(_contractPath);

            using var db = new AppDbContext();
            if (_user.Id == 0)
                db.Users.Add(_user);
            else
                db.Users.Update(_user);
            
            await db.SaveChangesAsync();

            await MessageBox.Show(this, _isEditMode ? "Данные сотрудника обновлены!" : "Сотрудник успешно добавлен!");
            Close();
        }
        catch (Exception ex)
        {
            ErrorTextBlock.Text = $"Ошибка сохранения: {ex.Message}";
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}