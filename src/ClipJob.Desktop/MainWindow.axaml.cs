using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace ClipJob.Desktop;

public sealed partial class MainWindow : Window
{
    private readonly PasteBackWorkflow? _pasteBackWorkflow;

    public MainWindow()
        : this([], null, null)
    {
    }

    internal MainWindow(
        IReadOnlyList<Clip> clips,
        IClipRepository? repository,
        IForegroundApplicationService? foregroundApplicationService)
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel(clips, repository);
        if (foregroundApplicationService is not null)
        {
            _pasteBackWorkflow = new PasteBackWorkflow(
                new AvaloniaClipboardService(this),
                foregroundApplicationService,
                new MacOSPasteService());
        }

        Opened += (_, _) => SearchTextBox.Focus();
    }

    private async void AddClip_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var editor = new ClipEditorWindow("Add Clip");
        var result = await editor.ShowDialog<ClipEditorResult?>(this);
        if (result is not null)
        {
            try
            {
                var error = await ((MainWindowViewModel)DataContext!).AddAsync(result.Label, result.Content);
                if (error is not null)
                {
                    await ShowErrorAsync(error);
                }
            }
            catch (Exception exception)
            {
                System.Diagnostics.Trace.TraceError($"Create failed: {exception}");
                await ShowErrorAsync("The clip could not be created.");
            }
        }
    }

    private async void EditClip_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var viewModel = (MainWindowViewModel)DataContext!;
        if (viewModel.SelectedClip is not { } clip)
        {
            return;
        }

        var editor = new ClipEditorWindow("Edit Clip", clip.Label, clip.Content);
        var result = await editor.ShowDialog<ClipEditorResult?>(this);
        if (result is not null)
        {
            try
            {
                var error = await viewModel.UpdateAsync(clip, result.Label, result.Content);
                if (error is not null)
                {
                    await ShowErrorAsync(error);
                }
            }
            catch (Exception exception)
            {
                System.Diagnostics.Trace.TraceError($"Update failed: {exception}");
                await ShowErrorAsync("The clip could not be updated.");
            }
        }
    }

    private async void DeleteClip_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var viewModel = (MainWindowViewModel)DataContext!;
        if (viewModel.SelectedClip is not { } clip)
        {
            return;
        }

        var confirmation = new ConfirmationWindow(clip.Label);
        if (await confirmation.ShowDialog<bool>(this))
        {
            try
            {
                await viewModel.DeleteAsync(clip);
            }
            catch (Exception exception)
            {
                System.Diagnostics.Trace.TraceError($"Delete failed: {exception}");
                await ShowErrorAsync("The clip could not be deleted.");
            }
        }
    }

    private async Task ShowErrorAsync(string message)
    {
        var dialog = new MessageWindow(message);
        await dialog.ShowDialog(this);
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
