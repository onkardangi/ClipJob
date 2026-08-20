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

    private static MainWindowViewModel CreateViewModel() => new(Clips);
}
