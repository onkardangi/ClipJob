using Avalonia.Controls;
using Avalonia.Input.Platform;

namespace ClipJob.Desktop;

public sealed class AvaloniaClipboardService(TopLevel topLevel) : IClipboardService
{
    public async Task<string?> GetTextAsync()
    {
        var clipboard = GetClipboard();
        return await clipboard.TryGetTextAsync();
    }

    public async Task SetTextAsync(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        await GetClipboard().SetTextAsync(text);
    }

    public async Task ClearAsync() => await GetClipboard().ClearAsync();

    private IClipboard GetClipboard() => topLevel.Clipboard
        ?? throw new InvalidOperationException("Avalonia did not provide a clipboard for the ClipJob window.");
}
