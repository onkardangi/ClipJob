using ClipJob.Desktop;
using Xunit;

namespace ClipJob.Desktop.Tests;

public sealed class MainWindowViewModelTests
{
    private static readonly IReadOnlyList<Clip> Clips =
    [
        new(Guid.NewGuid(), "email", "test@example.com"),
        new(Guid.NewGuid(), "linkedin", "https://linkedin.com/in/test"),
        new(Guid.NewGuid(), "experience", "Built high-throughput REST APIs...")
    ];

    [Fact]
    public void EmptyQueryShowsAllClipsAndSelectsFirst()
    {
        var viewModel = CreateViewModel();

        Assert.Equal(3, viewModel.VisibleClips.Count);
        Assert.Equal("email", viewModel.SelectedClip?.Label);
    }

    [Fact]
    public void QueryMatchesLabel()
    {
        var viewModel = CreateViewModel();
        viewModel.Query = "link";

        Assert.Equal("linkedin", Assert.Single(viewModel.VisibleClips).Label);
    }

    [Fact]
    public void QueryMatchesContent()
    {
        var viewModel = CreateViewModel();
        viewModel.Query = "test@";

        Assert.Equal("email", Assert.Single(viewModel.VisibleClips).Label);
    }

    [Fact]
    public void QueryMatchingIsCaseInsensitive()
    {
        var viewModel = CreateViewModel();
        viewModel.Query = "REST";

        Assert.Equal("experience", Assert.Single(viewModel.VisibleClips).Label);
    }

    [Fact]
    public void NoMatchClearsResultsAndSelection()
    {
        var viewModel = CreateViewModel();
        viewModel.Query = "missing";

        Assert.Empty(viewModel.VisibleClips);
        Assert.Null(viewModel.SelectedClip);
        Assert.True(viewModel.HasNoResults);
    }

    [Fact]
    public void FilteringResetsSelectionToFirstMatch()
    {
        var viewModel = CreateViewModel();
        viewModel.MoveSelectionDown();

        viewModel.Query = "e";

        Assert.Equal("email", viewModel.SelectedClip?.Label);
    }

    [Fact]
    public void MovingSelectionRespectsBoundaries()
    {
        var viewModel = CreateViewModel();

        viewModel.MoveSelectionUp();
        Assert.Equal("email", viewModel.SelectedClip?.Label);

        viewModel.MoveSelectionDown();
        viewModel.MoveSelectionDown();
        viewModel.MoveSelectionDown();
        Assert.Equal("experience", viewModel.SelectedClip?.Label);

        viewModel.MoveSelectionUp();
        Assert.Equal("linkedin", viewModel.SelectedClip?.Label);
    }

    [Fact]
    public void ResetClearsQueryAndRestoresFirstSelection()
    {
        var viewModel = CreateViewModel();
        viewModel.MoveSelectionDown();

        viewModel.Reset();

        Assert.Equal(string.Empty, viewModel.Query);
        Assert.Equal(3, viewModel.VisibleClips.Count);
        Assert.Equal("email", viewModel.SelectedClip?.Label);
    }

    [Fact]
    public async Task CreateMakesClipSearchable()
    {
        var repository = new FakeClipRepository(Clips);
        var viewModel = new MainWindowViewModel(Clips, repository) { Query = "new-role" };

        var error = await viewModel.AddAsync("new-role", "Software Engineer");

        Assert.Null(error);
        Assert.Equal("new-role", Assert.Single(viewModel.VisibleClips).Label);
        Assert.Contains(repository.Clips, clip => clip.Label == "new-role");
    }

    [Fact]
    public async Task EditUpdatesSearchResultsAndKeepsId()
    {
        var repository = new FakeClipRepository(Clips);
        var viewModel = new MainWindowViewModel(Clips, repository);
        var original = viewModel.SelectedClip!;

        var error = await viewModel.UpdateAsync(original, "contact-email", "new@example.com");
        viewModel.Query = "contact-email";

        Assert.Null(error);
        Assert.Equal(original.Id, Assert.Single(viewModel.VisibleClips).Id);
        Assert.Equal("new@example.com", viewModel.SelectedClip?.Content);
    }

    [Fact]
    public async Task DeleteSelectedMaintainsValidSelectionAndDeletingLastClearsIt()
    {
        var clips = Clips.Take(2).ToArray();
        var repository = new FakeClipRepository(clips);
        var viewModel = new MainWindowViewModel(clips, repository);

        await viewModel.DeleteAsync(viewModel.SelectedClip!);
        Assert.Equal("linkedin", viewModel.SelectedClip?.Label);

        await viewModel.DeleteAsync(viewModel.SelectedClip!);
        Assert.Empty(viewModel.VisibleClips);
        Assert.Null(viewModel.SelectedClip);
    }

    [Theory]
    [InlineData("", "content", "Label is required.")]
    [InlineData("   ", "content", "Label is required.")]
    [InlineData("label", "", "Content is required.")]
    [InlineData("label", "   ", "Content is required.")]
    public async Task InvalidCreateIsRejected(string label, string content, string expectedError)
    {
        var repository = new FakeClipRepository(Clips);
        var viewModel = new MainWindowViewModel(Clips, repository);

        Assert.Equal(expectedError, await viewModel.AddAsync(label, content));
        Assert.Equal(Clips.Count, repository.Clips.Count);
    }

    [Fact]
    public async Task DuplicateFailureDoesNotChangeInMemoryState()
    {
        var repository = new FakeClipRepository(Clips);
        var viewModel = new MainWindowViewModel(Clips, repository);

        var error = await viewModel.AddAsync("EMAIL", "Other");

        Assert.Equal("A clip with that label already exists.", error);
        Assert.Equal(Clips, viewModel.VisibleClips);
    }

    private static MainWindowViewModel CreateViewModel() => new(Clips);

    private sealed class FakeClipRepository(IEnumerable<Clip> clips) : IClipRepository
    {
        public List<Clip> Clips { get; } = [.. clips];
        public Task<IReadOnlyList<Clip>> GetAllAsync() => Task.FromResult<IReadOnlyList<Clip>>(Clips);
        public Task AddAsync(Clip clip) { Clips.Add(clip); return Task.CompletedTask; }
        public Task UpdateAsync(Clip clip)
        {
            var index = Clips.FindIndex(item => item.Id == clip.Id);
            Clips[index] = clip;
            return Task.CompletedTask;
        }
        public Task DeleteAsync(Guid id) { Clips.RemoveAll(clip => clip.Id == id); return Task.CompletedTask; }
    }
}
