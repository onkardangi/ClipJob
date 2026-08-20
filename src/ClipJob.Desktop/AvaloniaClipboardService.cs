using Avalonia.Controls;
using Avalonia.Input.Platform;

namespace ClipJob.Desktop;

public sealed class AvaloniaClipboardService(TopLevel topLevel) : IClipboardService
{
    public async Task SetTextAsync(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var clipboard = topLevel.Clipboard
            ?? throw new InvalidOperationException("Avalonia did not provide a clipboard for the ClipJob window.");

        await clipboard.SetTextAsync(text);
    }
}
