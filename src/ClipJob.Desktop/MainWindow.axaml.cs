using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace ClipJob.Desktop;

public sealed partial class MainWindow : Window
{
    private readonly PasteBackWorkflow? _pasteBackWorkflow;
    private readonly IMacOSOverlayWindowService? _overlayWindowService;

    public MainWindow()
        : this([], null, null, null)
    {
    }

    internal MainWindow(
        IReadOnlyList<Clip> clips,
        IClipRepository? repository,
        IForegroundApplicationService? foregroundApplicationService,
        IMacOSOverlayWindowService? overlayWindowService)
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel(clips, repository);
        _overlayWindowService = overlayWindowService;
        if (foregroundApplicationService is not null)
        {
            _pasteBackWorkflow = new PasteBackWorkflow(
                new AvaloniaClipboardService(this),
                foregroundApplicationService,
                new MacOSPasteService());
        }

        Opened += (_, _) =>
        {
            try
            {
                _overlayWindowService?.Configure(this);
            }
            catch (InvalidOperationException exception)
            {
                System.Diagnostics.Trace.TraceError($"macOS overlay configuration failed: {exception.Message}");
            }

            SearchTextBox.Focus();
        };
    }

    private void DragRegion_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private async void AddClip_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await OpenAddClipAsync();
    }

    private async Task OpenAddClipAsync()
    {
        var editor = new ClipEditorWindow("Add Clip");
        var result = await ShowOwnedDialogAsync<ClipEditorResult?>(editor);
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

        RestorePaletteFocus();
    }

    private async void EditClip_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Control { DataContext: Clip clip })
        {
            ((MainWindowViewModel)DataContext!).SelectedClip = clip;
        }

        await OpenEditClipAsync();
    }

    private async Task OpenEditClipAsync()
    {
        var viewModel = (MainWindowViewModel)DataContext!;
        if (viewModel.SelectedClip is not { } clip)
        {
            return;
        }

        var editor = new ClipEditorWindow("Edit Clip", clip.Label, clip.Content);
        var result = await ShowOwnedDialogAsync<ClipEditorResult?>(editor);
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

        RestorePaletteFocus();
    }

    private async void DeleteClip_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Control { DataContext: Clip clip })
        {
            ((MainWindowViewModel)DataContext!).SelectedClip = clip;
        }

        await RequestDeleteClipAsync();
    }

    private async Task RequestDeleteClipAsync()
    {
        var viewModel = (MainWindowViewModel)DataContext!;
        if (viewModel.SelectedClip is not { } clip)
        {
            return;
        }

        var confirmation = new ConfirmationWindow(clip.Label);
        if (await ShowOwnedDialogAsync<bool>(confirmation))
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

        RestorePaletteFocus();
    }

    private async Task ShowErrorAsync(string message)
    {
        var dialog = new MessageWindow(message);
        await ShowOwnedDialogAsync<object?>(dialog);
    }

    private async Task<T> ShowOwnedDialogAsync<T>(Window dialog)
    {
        _overlayWindowService?.SetFloating(this, false);
        try
        {
            return await dialog.ShowDialog<T>(this);
        }
        finally
        {
            _overlayWindowService?.SetFloating(this, true);
        }
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

        if (_overlayWindowService is not null)
        {
            try
            {
                _overlayWindowService.OrderFront(this);
            }
            catch (InvalidOperationException exception)
            {
                System.Diagnostics.Trace.TraceError($"macOS overlay ordering failed: {exception.Message}");
                Activate();
            }
        }
        else
        {
            Activate();
        }
        Dispatcher.UIThread.Post(() => SearchTextBox.Focus(), DispatcherPriority.Input);
    }

    private async void Window_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Meta))
        {
            return;
        }

        switch (e.Key)
        {
            case Key.N:
                e.Handled = true;
                await OpenAddClipAsync();
                break;
            case Key.E:
                e.Handled = true;
                await OpenEditClipAsync();
                break;
            case Key.Back:
                e.Handled = true;
                await RequestDeleteClipAsync();
                break;
        }
    }

    private void ResultsListBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (((MainWindowViewModel)DataContext!).SelectedClip is { } selectedClip)
        {
            ResultsListBox.ScrollIntoView(selectedClip);
        }
    }

    private void RestorePaletteFocus() =>
        Dispatcher.UIThread.Post(() => SearchTextBox.Focus(), DispatcherPriority.Input);

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
                        if (!await _pasteBackWorkflow.ExecuteAsync(viewModel.SelectedClip, Hide))
                        {
                            Show();
                            Activate();
                            await ShowErrorAsync(
                                "ClipJob could not return to the previous application. Summon ClipJob with ⌘⇧V while the destination field is focused, then try again.");
                            RestorePaletteFocus();
                        }
                    }
                    catch (InvalidOperationException exception)
                    {
                        System.Diagnostics.Trace.TraceError($"Paste-back failed: {exception}");
                        Show();
                        Activate();
                        await ShowErrorAsync(exception.Message);
                        RestorePaletteFocus();
                    }
                    catch (Exception exception)
                    {
                        System.Diagnostics.Trace.TraceError($"Paste-back failed: {exception}");
                        Show();
                        Activate();
                        await ShowErrorAsync("ClipJob could not paste the selected clip.");
                        RestorePaletteFocus();
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
