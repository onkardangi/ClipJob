namespace ClipJob.Desktop;

public interface IClipboardService
{
    Task SetTextAsync(string text);
}
