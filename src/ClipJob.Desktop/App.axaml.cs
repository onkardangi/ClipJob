using Avalonia;
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

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (OperatingSystem.IsMacOS())
            {
                _foregroundApplicationService = new MacOSForegroundApplicationService();
            }

            var mainWindow = new MainWindow(_foregroundApplicationService);
            desktop.MainWindow = mainWindow;

            if (OperatingSystem.IsMacOS())
            {
                _globalHotkeyService = new MacOSGlobalHotkeyService();

                try
                {
                    _globalHotkeyService.Register(
                        () =>
                        {
                            _foregroundApplicationService!.CaptureCurrentApplication();
                            Dispatcher.UIThread.Post(mainWindow.Summon);
                        });
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
}
