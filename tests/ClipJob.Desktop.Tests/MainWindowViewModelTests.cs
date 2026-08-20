using ClipJob.Desktop;
using Xunit;

namespace ClipJob.Desktop.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void EmptyQueryShowsAllClipsAndSelectsFirst()
    {
        var viewModel = new MainWindowViewModel();

        Assert.Equal(3, viewModel.VisibleClips.Count);
        Assert.Equal("email", viewModel.SelectedClip?.Label);
    }

    [Fact]
    public void QueryMatchesLabel()
    {
        var viewModel = new MainWindowViewModel { Query = "link" };

        Assert.Equal("linkedin", Assert.Single(viewModel.VisibleClips).Label);
    }

    [Fact]
    public void QueryMatchesContent()
    {
        var viewModel = new MainWindowViewModel { Query = "test@" };

        Assert.Equal("email", Assert.Single(viewModel.VisibleClips).Label);
    }

    [Fact]
    public void QueryMatchingIsCaseInsensitive()
    {
        var viewModel = new MainWindowViewModel { Query = "REST" };

        Assert.Equal("experience", Assert.Single(viewModel.VisibleClips).Label);
    }

    [Fact]
    public void NoMatchClearsResultsAndSelection()
    {
        var viewModel = new MainWindowViewModel { Query = "missing" };

        Assert.Empty(viewModel.VisibleClips);
        Assert.Null(viewModel.SelectedClip);
    }

    [Fact]
    public void FilteringResetsSelectionToFirstMatch()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.MoveSelectionDown();

        viewModel.Query = "e";

        Assert.Equal("email", viewModel.SelectedClip?.Label);
    }

    [Fact]
    public void MovingSelectionRespectsBoundaries()
    {
        var viewModel = new MainWindowViewModel();

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
        var viewModel = new MainWindowViewModel();
        viewModel.MoveSelectionDown();

        viewModel.Reset();

        Assert.Equal(string.Empty, viewModel.Query);
        Assert.Equal(3, viewModel.VisibleClips.Count);
        Assert.Equal("email", viewModel.SelectedClip?.Label);
    }
}
