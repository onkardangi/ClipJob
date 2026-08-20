using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace ClipJob.Desktop;

public sealed partial class MainWindow : Window
{
    private readonly IForegroundApplicationService? _foregroundApplicationService;

    public MainWindow()
        : this(null)
    {
    }

    internal MainWindow(IForegroundApplicationService? foregroundApplicationService)
    {
        _foregroundApplicationService = foregroundApplicationService;
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

        // Temporary Milestone 4 verification action; Milestone 5 will own restoration.
        if (e.Key == Key.R && e.KeyModifiers.HasFlag(KeyModifiers.Meta))
        {
            _foregroundApplicationService?.RestoreCapturedApplication();
            e.Handled = true;
            return;
        }

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
