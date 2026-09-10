using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace ClipJob.Desktop;

public sealed partial class SettingsWindow : Window
{
    private GlobalShortcut _shortcut;
    private bool _isRecording;

    public SettingsWindow()
        : this(GlobalShortcut.Default, false, null)
    {
    }

    public SettingsWindow(GlobalShortcut shortcut, bool accessibilityGranted, string? hotkeyError)
    {
        InitializeComponent();
        _shortcut = shortcut;
        UpdateShortcutButton();

        AccessibilityIndicator.Fill = accessibilityGranted ? Brushes.ForestGreen : Brushes.DarkOrange;
        AccessibilityDetailText.Text = accessibilityGranted
            ? "Granted — ClipJob can paste into other applications."
            : "Not granted — paste-back will not work.";
        OpenAccessibilityButton.IsVisible = !accessibilityGranted;

        HotkeyIndicator.Fill = hotkeyError is null ? Brushes.ForestGreen : Brushes.DarkOrange;
        HotkeyDetailText.Text = hotkeyError ?? $"Registered as {_shortcut.DisplayText}.";
    }

    private void ShortcutButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _isRecording = true;
        ShortcutErrorText.Text = string.Empty;
        ShortcutButton.Content = "Press shortcut…";
        ShortcutButton.Focus();
    }

    private void Window_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (!_isRecording)
        {
            if (e.Key == Key.Escape)
            {
                Close(null);
            }
            return;
        }

        e.Handled = true;
        if (e.Key == Key.Escape)
        {
            _isRecording = false;
            UpdateShortcutButton();
            return;
        }

        var candidate = new GlobalShortcut(e.Key, e.KeyModifiers);
        if (!candidate.IsValid)
        {
            ShortcutErrorText.Text = "Use Command plus a letter; Shift, Option, and Control are optional.";
            return;
        }

        _shortcut = candidate;
        _isRecording = false;
        UpdateShortcutButton();
    }

    private void UpdateShortcutButton() => ShortcutButton.Content = _shortcut.DisplayText;

    private void Save_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close(_shortcut);

    private void Cancel_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close(null);

    private void OpenAccessibility_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (OperatingSystem.IsMacOS())
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "open",
                ArgumentList = { "x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility" },
                UseShellExecute = false
            });
        }
    }
}
