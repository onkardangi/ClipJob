using ClipJob.Desktop;
using Xunit;

namespace ClipJob.Desktop.Tests;

public sealed class PasteBackWorkflowTests
{
    [Fact]
    public async Task ExecutePreservesClipboardAroundPasteBack()
    {
        var operations = new List<string>();
        var clipboard = new RecordingClipboard(operations, "Amazon");
        var workflow = CreateWorkflow(clipboard, new RecordingForegroundApplication(operations, true), new RecordingPaste(operations));

        var completed = await workflow.ExecuteAsync(new Clip(Guid.NewGuid(), "email", "test@example.com"), () => operations.Add("hide"));

        Assert.True(completed);
        Assert.Equal(
            ["clipboard:get", "clipboard:set:test@example.com", "hide", "restore", "paste", "wait", "clipboard:get", "clipboard:set:Amazon"],
            operations);
        Assert.Equal("Amazon", clipboard.Text);
    }

    [Fact]
    public async Task NoSelectedClipDoesNothing()
    {
        var operations = new List<string>();
        var workflow = CreateWorkflow(new RecordingClipboard(operations, "Amazon"), new RecordingForegroundApplication(operations, true), new RecordingPaste(operations));

        var completed = await workflow.ExecuteAsync(null, () => operations.Add("hide"));

        Assert.False(completed);
        Assert.Empty(operations);
    }

    [Fact]
    public async Task NoCapturedApplicationDoesNothing()
    {
        var operations = new List<string>();
        var workflow = CreateWorkflow(new RecordingClipboard(operations, "Amazon"), new RecordingForegroundApplication(operations, false), new RecordingPaste(operations));

        var completed = await workflow.ExecuteAsync(new Clip(Guid.NewGuid(), "email", "test@example.com"), () => operations.Add("hide"));

        Assert.False(completed);
        Assert.Empty(operations);
    }

    [Fact]
    public async Task FailedActivationRestoresPreviousClipboardWithoutPasting()
    {
        var operations = new List<string>();
        var clipboard = new RecordingClipboard(operations, "Amazon");
        var workflow = CreateWorkflow(clipboard, new RecordingForegroundApplication(operations, true, false), new RecordingPaste(operations));

        var completed = await workflow.ExecuteAsync(new Clip(Guid.NewGuid(), "email", "test@example.com"), () => operations.Add("hide"));

        Assert.False(completed);
        Assert.Equal(
            ["clipboard:get", "clipboard:set:test@example.com", "hide", "restore", "clipboard:get", "clipboard:set:Amazon"],
            operations);
        Assert.Equal("Amazon", clipboard.Text);
    }

    [Fact]
    public async Task MissingPreviousTextClearsTemporaryTextAfterPaste()
    {
        var operations = new List<string>();
        var clipboard = new RecordingClipboard(operations, null);
        var workflow = CreateWorkflow(clipboard, new RecordingForegroundApplication(operations, true), new RecordingPaste(operations));

        var completed = await workflow.ExecuteAsync(new Clip(Guid.NewGuid(), "email", "test@example.com"), () => operations.Add("hide"));

        Assert.True(completed);
        Assert.Null(clipboard.Text);
        Assert.Equal("clipboard:clear", operations[^1]);
    }

    [Fact]
    public async Task NewerClipboardTextIsNotOverwritten()
    {
        var operations = new List<string>();
        var clipboard = new RecordingClipboard(operations, "Amazon");
        var workflow = new PasteBackWorkflow(
            clipboard,
            new RecordingForegroundApplication(operations, true),
            new RecordingPaste(operations),
            () =>
            {
                operations.Add("wait");
                clipboard.ReplaceExternally("Stripe");
                return Task.CompletedTask;
            });

        var completed = await workflow.ExecuteAsync(new Clip(Guid.NewGuid(), "email", "test@example.com"), () => operations.Add("hide"));

        Assert.True(completed);
        Assert.Equal("Stripe", clipboard.Text);
        Assert.DoesNotContain("clipboard:set:Amazon", operations);
    }

    [Fact]
    public async Task RestorationFailureDoesNotFailSuccessfulPaste()
    {
        var operations = new List<string>();
        var clipboard = new RecordingClipboard(operations, "Amazon") { FailRestoration = true };
        var workflow = CreateWorkflow(clipboard, new RecordingForegroundApplication(operations, true), new RecordingPaste(operations));

        var completed = await workflow.ExecuteAsync(new Clip(Guid.NewGuid(), "email", "test@example.com"), () => operations.Add("hide"));

        Assert.True(completed);
        Assert.Equal("test@example.com", clipboard.Text);
    }

    [Fact]
    public async Task PasteFailureRestoresPreviousClipboardAndPropagates()
    {
        var operations = new List<string>();
        var clipboard = new RecordingClipboard(operations, "Amazon");
        var workflow = CreateWorkflow(
            clipboard,
            new RecordingForegroundApplication(operations, true),
            new FailingPaste(operations));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            workflow.ExecuteAsync(new Clip(Guid.NewGuid(), "email", "test@example.com"), () => operations.Add("hide")));

        Assert.Equal(
            ["clipboard:get", "clipboard:set:test@example.com", "hide", "restore", "paste", "clipboard:get", "clipboard:set:Amazon"],
            operations);
        Assert.Equal("Amazon", clipboard.Text);
    }

    [Fact]
    public async Task OverlappingExecutionsAreSerialized()
    {
        var operations = new List<string>();
        var clipboard = new RecordingClipboard(operations, "Original");
        var firstCanFinish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var waitStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var waitCount = 0;
        var workflow = new PasteBackWorkflow(
            clipboard,
            new RecordingForegroundApplication(operations, true),
            new RecordingPaste(operations),
            async () =>
            {
                operations.Add("wait");
                if (Interlocked.Increment(ref waitCount) == 1)
                {
                    waitStarted.SetResult();
                    await firstCanFinish.Task;
                }
            });

        var first = workflow.ExecuteAsync(new Clip(Guid.NewGuid(), "email", "first"), () => operations.Add("hide:first"));
        await waitStarted.Task;
        var second = workflow.ExecuteAsync(new Clip(Guid.NewGuid(), "email", "second"), () => operations.Add("hide:second"));

        Assert.DoesNotContain("clipboard:set:second", operations);
        firstCanFinish.SetResult();

        Assert.True(await first);
        Assert.True(await second);
        Assert.Equal("Original", clipboard.Text);
    }

    private static PasteBackWorkflow CreateWorkflow(RecordingClipboard clipboard, IForegroundApplicationService foregroundApplication, IPasteService pasteService) =>
        new(clipboard, foregroundApplication, pasteService, () =>
        {
            clipboard.Operations.Add("wait");
            return Task.CompletedTask;
        });

    private sealed class RecordingClipboard(List<string> operations, string? initialText) : IClipboardService
    {
        private bool _temporaryTextWasWritten;

        public List<string> Operations => operations;
        public string? Text { get; private set; } = initialText;
        public bool FailRestoration { get; init; }

        public Task<string?> GetTextAsync()
        {
            operations.Add("clipboard:get");
            return Task.FromResult(Text);
        }

        public Task SetTextAsync(string text)
        {
            if (FailRestoration && _temporaryTextWasWritten)
            {
                throw new InvalidOperationException("Restoration failed.");
            }

            operations.Add($"clipboard:set:{text}");
            Text = text;
            _temporaryTextWasWritten = true;
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            operations.Add("clipboard:clear");
            Text = null;
            return Task.CompletedTask;
        }

        public void ReplaceExternally(string text) => Text = text;
    }

    private sealed class RecordingForegroundApplication(List<string> operations, bool hasCapturedApplication, bool activationSucceeds = true) : IForegroundApplicationService
    {
        public bool HasCapturedApplication => hasCapturedApplication;
        public void CaptureCurrentApplication() { }

        public Task<bool> RestoreCapturedApplicationAsync()
        {
            operations.Add("restore");
            return Task.FromResult(activationSucceeds);
        }

        public void Dispose() { }
    }

    private sealed class RecordingPaste(List<string> operations) : IPasteService
    {
        public void Paste() => operations.Add("paste");
    }

    private sealed class FailingPaste(List<string> operations) : IPasteService
    {
        public void Paste()
        {
            operations.Add("paste");
            throw new InvalidOperationException("Paste failed.");
        }
    }
}
