using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace MyApp.Views
{
    public partial class ConfirmationDialogWindow : Window
    {
        public bool Result { get; private set; } = false;
        
        public ConfirmationDialogWindow()
        {
            InitializeComponent();
            this.AttachDevTools();
        }

        public ConfirmationDialogWindow(string message, string title) : this()
        {
            this.Title = title;
            var messageTextBlock = this.FindControl<TextBlock>("MessageText");
            if (messageTextBlock != null)
                messageTextBlock.Text = message;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            Close();
        }
    }
}