namespace ClipJob.Desktop;

public interface IForegroundApplicationService : IDisposable
{
    bool HasCapturedApplication { get; }

    int CapturedProcessIdentifier { get; }

    void CaptureCurrentApplication();

    Task<bool> RestoreCapturedApplicationAsync();
}
