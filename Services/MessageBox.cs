using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using System.Threading.Tasks;

namespace MyApp.Services;
public static class MessageBox
{
    public static Task Show(Window parent, string text)
    {
        var msgbox = new Window
        {
            Title = "Сообщение",
            Width = 300,
            Height = 150,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var stack = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 10
        };

        stack.Children.Add(new TextBlock
        {
            Text = text,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        });

        var button = new Button
        {
            Content = "OK",
            HorizontalAlignment = HorizontalAlignment.Center
        };

        button.Click += (s, e) => msgbox.Close();

        stack.Children.Add(button);

        msgbox.Content = stack;

        return msgbox.ShowDialog(parent);
    }
}