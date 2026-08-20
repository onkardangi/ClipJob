using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace ClipJob.Desktop;

public sealed partial class MainWindow : Window
{
    private readonly PasteBackWorkflow? _pasteBackWorkflow;

    public MainWindow()
        : this([], null)
    {
    }

    internal MainWindow(
        IReadOnlyList<Clip> clips,
        IForegroundApplicationService? foregroundApplicationService)
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel(clips);
        if (foregroundApplicationService is not null)
        {
            _pasteBackWorkflow = new PasteBackWorkflow(
                new AvaloniaClipboardService(this),
                foregroundApplicationService,
                new MacOSPasteService());
        }

        Opened += (_, _) => SearchTextBox.Focus();
    }

    public void Summon()
    {
        ((MainWindowViewModel)DataContext!).Reset();

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

    private async void SearchTextBox_OnKeyDown(object? sender, KeyEventArgs e)
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
                e.Handled = true;
                if (_pasteBackWorkflow is not null)
                {
                    try
                    {
                        await _pasteBackWorkflow.ExecuteAsync(viewModel.SelectedClip, Hide);
                    }
                    catch (Exception exception)
                    {
                        System.Diagnostics.Trace.TraceError($"Paste-back failed: {exception}");
                    }
                }
                break;
            case Key.Escape:
                Hide();
                e.Handled = true;
                break;
        }
    }
}
