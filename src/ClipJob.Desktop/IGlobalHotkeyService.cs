namespace ClipJob.Desktop;

public interface IGlobalHotkeyService : IDisposable
{
    void Register(Action onPressed);
}
