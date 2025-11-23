using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace MyApp.Views;

public class StatusToBrushConverter : IValueConverter
{
    public static readonly StatusToBrushConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string status) return Brushes.Gray;

        return status switch
        {
            "New" => Brushes.Orange,
            "Accepted" => Brushes.DodgerBlue,
            "Paid" => Brushes.Green,
            _ => Brushes.Gray
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}