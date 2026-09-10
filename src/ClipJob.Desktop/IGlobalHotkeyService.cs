namespace ClipJob.Desktop;

public interface IGlobalHotkeyService : IDisposable
{
    void Register(GlobalShortcut shortcut, Action onPressed);
}
