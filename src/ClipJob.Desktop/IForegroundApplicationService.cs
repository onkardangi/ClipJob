namespace ClipJob.Desktop;

public interface IForegroundApplicationService : IDisposable
{
    void CaptureCurrentApplication();

    void RestoreCapturedApplication();
}
