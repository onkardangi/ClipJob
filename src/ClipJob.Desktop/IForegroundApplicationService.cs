namespace ClipJob.Desktop;

public interface IForegroundApplicationService : IDisposable
{
    bool HasCapturedApplication { get; }

    void CaptureCurrentApplication();

    Task<bool> RestoreCapturedApplicationAsync();
}
