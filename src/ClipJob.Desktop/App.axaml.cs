using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System.Diagnostics;

namespace ClipJob.Desktop;

public sealed partial class App : Application
{
    private IGlobalHotkeyService? _globalHotkeyService;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            desktop.MainWindow = mainWindow;

            if (OperatingSystem.IsMacOS())
            {
                _globalHotkeyService = new MacOSGlobalHotkeyService();

                try
                {
                    _globalHotkeyService.Register(
                        () => Dispatcher.UIThread.Post(mainWindow.Summon));
                }
                catch (InvalidOperationException exception)
                {
                    Trace.TraceError(exception.Message);
                }

                desktop.Exit += (_, _) => _globalHotkeyService.Dispose();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
