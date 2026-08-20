using ClipJob.Desktop;
using Xunit;

namespace ClipJob.Desktop.Tests;

public sealed class PasteBackWorkflowTests
{
    [Fact]
    public async Task ExecutePreservesPasteBackOrdering()
    {
        var operations = new List<string>();
        var workflow = new PasteBackWorkflow(
            new RecordingClipboard(operations),
            new RecordingForegroundApplication(operations, hasCapturedApplication: true),
            new RecordingPaste(operations));

        var completed = await workflow.ExecuteAsync(
            new Clip("email", "test@example.com"),
            () => operations.Add("hide"));

        Assert.True(completed);
        Assert.Equal(["clipboard:test@example.com", "hide", "restore", "paste"], operations);
    }

    [Fact]
    public async Task NoSelectedClipDoesNothing()
    {
        var operations = new List<string>();
        var workflow = new PasteBackWorkflow(
            new RecordingClipboard(operations),
            new RecordingForegroundApplication(operations, hasCapturedApplication: true),
            new RecordingPaste(operations));

        var completed = await workflow.ExecuteAsync(null, () => operations.Add("hide"));

        Assert.False(completed);
        Assert.Empty(operations);
    }

    [Fact]
    public async Task NoCapturedApplicationDoesNothing()
    {
        var operations = new List<string>();
        var workflow = new PasteBackWorkflow(
            new RecordingClipboard(operations),
            new RecordingForegroundApplication(operations, hasCapturedApplication: false),
            new RecordingPaste(operations));

        var completed = await workflow.ExecuteAsync(
            new Clip("email", "test@example.com"),
            () => operations.Add("hide"));

        Assert.False(completed);
        Assert.Empty(operations);
    }

    [Fact]
    public async Task FailedActivationDoesNotPaste()
    {
        var operations = new List<string>();
        var workflow = new PasteBackWorkflow(
            new RecordingClipboard(operations),
            new RecordingForegroundApplication(
                operations,
                hasCapturedApplication: true,
                activationSucceeds: false),
            new RecordingPaste(operations));

        var completed = await workflow.ExecuteAsync(
            new Clip("email", "test@example.com"),
            () => operations.Add("hide"));

        Assert.False(completed);
        Assert.Equal(["clipboard:test@example.com", "hide", "restore"], operations);
    }

    private sealed class RecordingClipboard(List<string> operations) : IClipboardService
    {
        public Task SetTextAsync(string text)
        {
            operations.Add($"clipboard:{text}");
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingForegroundApplication(
        List<string> operations,
        bool hasCapturedApplication,
        bool activationSucceeds = true) : IForegroundApplicationService
    {
        public bool HasCapturedApplication => hasCapturedApplication;

        public void CaptureCurrentApplication()
        {
        }

        public Task<bool> RestoreCapturedApplicationAsync()
        {
            operations.Add("restore");
            return Task.FromResult(activationSucceeds);
        }

        public void Dispose()
        {
        }
    }

    private sealed class RecordingPaste(List<string> operations) : IPasteService
    {
        public void Paste() => operations.Add("paste");
    }
}
