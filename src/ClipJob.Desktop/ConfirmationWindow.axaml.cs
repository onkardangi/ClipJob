using Avalonia.Controls;
using Avalonia.Input;

namespace ClipJob.Desktop;

public sealed partial class ConfirmationWindow : Window
{
    public ConfirmationWindow()
        : this(string.Empty)
    {
    }

    public ConfirmationWindow(string label)
    {
        InitializeComponent();
        Question.Text = $"Delete “{label}”?";
        Opened += (_, _) => CancelButton.Focus();
    }

    private void Cancel_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close(false);
    private void Delete_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close(true);

    private void Window_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close(false);
        }
    }
}
