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

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var clipRepository = SqliteClipRepository.CreateDefault();
            await clipRepository.InitializeAsync();
            var clips = await clipRepository.GetAllAsync();

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
                        () => Summon(mainWindow));
                }
                catch (InvalidOperationException exception)
                {
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

    private void Summon(MainWindow mainWindow)
    {
        _foregroundApplicationService?.CaptureCurrentApplication();
        Dispatcher.UIThread.Post(mainWindow.Summon);
    }
}
