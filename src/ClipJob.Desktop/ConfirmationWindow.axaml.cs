using Avalonia.Controls;

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
        Question.Text = $"Delete \"{label}\"?";
    }

    private void Cancel_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close(false);
    private void Delete_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close(true);
}
