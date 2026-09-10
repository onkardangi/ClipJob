using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System.Diagnostics;

namespace ClipJob.Desktop;

public sealed partial class App : Application
{
    private IGlobalHotkeyService? _globalHotkeyService;
    private IForegroundApplicationService? _foregroundApplicationService;
    private readonly AppSettingsStore _settingsStore = AppSettingsStore.CreateDefault();
    private AppSettings _settings = new();
    private string? _hotkeyError;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var clipRepository = SqliteClipRepository.CreateDefault();
            await clipRepository.InitializeAsync();
            var clips = await clipRepository.GetAllAsync();
            _settings = await _settingsStore.LoadAsync();
            if (!_settings.Shortcut.IsValid)
            {
                _settings.Shortcut = GlobalShortcut.Default;
            }

            if (OperatingSystem.IsMacOS())
            {
                _foregroundApplicationService = new MacOSForegroundApplicationService();
            }

            var overlayWindowService = OperatingSystem.IsMacOS()
                ? new MacOSOverlayWindowService()
                : null;
            var mainWindow = new MainWindow(
                clips,
                clipRepository,
                _foregroundApplicationService,
                overlayWindowService);
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            desktop.MainWindow = mainWindow;

            if (OperatingSystem.IsMacOS())
            {
                _globalHotkeyService = new MacOSGlobalHotkeyService();

                try
                {
                    _globalHotkeyService.Register(
                        _settings.Shortcut,
                        () => Summon(mainWindow));
                }
                catch (InvalidOperationException exception)
                {
                    _hotkeyError = exception.Message;
                    Trace.TraceError(exception.Message);
                }

                desktop.Exit += (_, _) =>
                {
                    _globalHotkeyService.Dispose();
                    _foregroundApplicationService?.Dispose();
                };
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ShowClipJob_OnClick(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: MainWindow mainWindow })
        {
            Summon(mainWindow);
        }
    }

    private void QuitClipJob_OnClick(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.TryShutdown();
        }
    }

    private async void Settings_OnClick(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime { MainWindow: MainWindow mainWindow })
        {
            return;
        }

        var settingsWindow = new SettingsWindow(
            _settings.Shortcut,
            !OperatingSystem.IsMacOS() || MacOSPasteService.IsAccessibilityGranted,
            _hotkeyError);
        Summon(mainWindow);
        var shortcut = await settingsWindow.ShowDialog<GlobalShortcut?>(mainWindow);
        if (shortcut is null || shortcut == _settings.Shortcut)
        {
            return;
        }

        try
        {
            _globalHotkeyService?.Register(shortcut, () => Summon(mainWindow));
            _settings.Shortcut = shortcut;
            await _settingsStore.SaveAsync(_settings);
            _hotkeyError = null;
        }
        catch (InvalidOperationException exception)
        {
            _hotkeyError = exception.Message;
            Trace.TraceError(exception.Message);
            try
            {
                _globalHotkeyService?.Register(_settings.Shortcut, () => Summon(mainWindow));
            }
            catch (InvalidOperationException restoreException)
            {
                _hotkeyError = restoreException.Message;
                Trace.TraceError(restoreException.Message);
            }

            await new MessageWindow(exception.Message).ShowDialog<object?>(mainWindow);
        }
    }

    private void Summon(MainWindow mainWindow)
    {
        _foregroundApplicationService?.CaptureCurrentApplication();
        Dispatcher.UIThread.Post(mainWindow.Summon);
    }
}
