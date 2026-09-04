using System.Diagnostics;

namespace ClipJob.Desktop;

public sealed class PasteBackWorkflow(
    IClipboardService clipboardService,
    IForegroundApplicationService foregroundApplicationService,
    IPasteService pasteService,
    Func<Task>? waitForPasteConsumption = null,
    Func<Task>? waitForTargetFocus = null)
{
    private static readonly TimeSpan TargetFocusDelay = TimeSpan.FromMilliseconds(350);
    private static readonly TimeSpan PasteConsumptionDelay = TimeSpan.FromMilliseconds(500);
    private readonly SemaphoreSlim _executionLock = new(1, 1);
    private readonly Func<Task> _waitForPasteConsumption = waitForPasteConsumption
        ?? (() => Task.Delay(PasteConsumptionDelay));
    private readonly Func<Task> _waitForTargetFocus = waitForTargetFocus
        ?? (() => Task.Delay(TargetFocusDelay));

    public async Task<bool> ExecuteAsync(Clip? clip, Action hideClipJob)
    {
        ArgumentNullException.ThrowIfNull(hideClipJob);

        if (clip is null)
        {
            return false;
        }

        if (!foregroundApplicationService.HasCapturedApplication)
        {
            Trace.TraceWarning("No external application was captured for paste-back.");
            return false;
        }

        await _executionLock.WaitAsync();
        try
        {
            var previousText = await clipboardService.GetTextAsync();
            await clipboardService.SetTextAsync(clip.Content);

            try
            {
                hideClipJob();

                if (!await foregroundApplicationService.RestoreCapturedApplicationAsync())
                {
                    return false;
                }

                // macOS can report the application as frontmost before its previous
                // control has regained keyboard focus, especially across Spaces.
                await _waitForTargetFocus();
                pasteService.Paste();

                // CGEventPost does not acknowledge when the target has consumed the
                // clipboard, so restoration needs a short, bounded grace period.
                await _waitForPasteConsumption();
                return true;
            }
            finally
            {
                await RestoreClipboardAsync(clip.Content, previousText);
            }
        }
        finally
        {
            _executionLock.Release();
        }
    }

    private async Task RestoreClipboardAsync(string temporaryText, string? previousText)
    {
        try
        {
            var currentText = await clipboardService.GetTextAsync();
            if (currentText != temporaryText)
            {
                Trace.TraceWarning("The clipboard changed during paste-back; the newer contents were left unchanged.");
                return;
            }

            if (previousText is null)
            {
                // This milestone snapshots text only. Clearing removes ClipJob's
                // temporary text but cannot reconstruct prior non-text formats.
                await clipboardService.ClearAsync();
            }
            else
            {
                await clipboardService.SetTextAsync(previousText);
            }
        }
        catch (Exception exception)
        {
            Trace.TraceError($"Clipboard restoration failed: {exception}");
        }
    }
}
