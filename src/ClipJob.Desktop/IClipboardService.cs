namespace ClipJob.Desktop;

public interface IClipboardService
{
    Task<string?> GetTextAsync();

    Task SetTextAsync(string text);

    Task ClearAsync();
}
