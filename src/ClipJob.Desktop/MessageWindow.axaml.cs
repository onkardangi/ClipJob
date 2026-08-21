using Avalonia.Controls;
using Avalonia.Input;

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
        Opened += (_, _) => OkButton.Focus();
    }

    private void Ok_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();

    private void Window_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Escape or Key.Enter)
        {
            e.Handled = true;
            Close();
        }
    }
}
