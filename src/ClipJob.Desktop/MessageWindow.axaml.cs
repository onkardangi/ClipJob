using Avalonia.Controls;

namespace ClipJob.Desktop;

public sealed partial class MessageWindow : Window
{
    public MessageWindow()
        : this(string.Empty)
    {
    }

    public MessageWindow(string message)
    {
        InitializeComponent();
        Message.Text = message;
    }

    private void Ok_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();
}
