using Avalonia.Controls;

namespace ClipJob.Desktop;

public interface IMacOSOverlayWindowService
{
    void Configure(Window window);

    void PositionForApplication(Window window, int processIdentifier);

    void OrderFront(Window window);

    void SetFloating(Window window, bool floating);
}
