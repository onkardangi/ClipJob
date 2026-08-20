using System.Diagnostics;

namespace ClipJob.Desktop;

public sealed class PasteBackWorkflow(
    IClipboardService clipboardService,
    IForegroundApplicationService foregroundApplicationService,
    IPasteService pasteService)
{
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

        await clipboardService.SetTextAsync(clip.Content);
        hideClipJob();

        if (!await foregroundApplicationService.RestoreCapturedApplicationAsync())
        {
            return false;
        }

        pasteService.Paste();
        return true;
    }
}
