using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia;
using System.Threading.Tasks;

namespace MyApp.Services;

public static class MessageBox
{

    public static Task Show(Window parent, string text, string title = "Сообщение")
    {
        return ShowInternal(parent, text, title, false);
    }
    
    public static Task<bool> Show(Window parent, string text, string title, bool yesNo)
    {
        return ShowInternal(parent, text, title, yesNo);
    }

    private static async Task<bool> ShowInternal(Window parent, string text, string title, bool yesNo)
    {
        var msgbox = new Window
        {
            Title = title,
            Width = 360,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var stack = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 15
        };

        stack.Children.Add(new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        var buttonsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 12
        };

        bool result = false;

        if (yesNo)
        {
            var yesBtn = new Button { Content = "Да", Width = 80 };
            var noBtn = new Button { Content = "Нет", Width = 80 };

            yesBtn.Click += (_, __) => { result = true; msgbox.Close(); };
            noBtn.Click += (_, __) => msgbox.Close();

            buttonsPanel.Children.Add(yesBtn);
            buttonsPanel.Children.Add(noBtn);
        }
        else
        {
            var okBtn = new Button { Content = "ОК", Width = 80 };
            okBtn.Click += (_, __) => msgbox.Close();
            buttonsPanel.Children.Add(okBtn);
        }

        stack.Children.Add(buttonsPanel);
        msgbox.Content = stack;

        await msgbox.ShowDialog(parent);
        return result;
    }
}