using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace ClipJob.Desktop;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
        Opened += (_, _) => SearchTextBox.Focus();
    }

    public void Summon()
    {
        if (!IsVisible)
        {
            Show();
        }

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
        Dispatcher.UIThread.Post(() => SearchTextBox.Focus(), DispatcherPriority.Input);
    }

    private void SearchTextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        var viewModel = (MainWindowViewModel)DataContext!;

        switch (e.Key)
        {
            case Key.Down:
                viewModel.MoveSelectionDown();
                e.Handled = true;
                break;
            case Key.Up:
                viewModel.MoveSelectionUp();
                e.Handled = true;
                break;
            case Key.Enter:
                viewModel.ConfirmSelection();
                e.Handled = true;
                break;
            case Key.Escape:
                Hide();
                e.Handled = true;
                break;
        }
    }
}
