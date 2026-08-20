using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ClipJob.Desktop;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly IReadOnlyList<Clip> _allClips;
    private string _query = string.Empty;
    private IReadOnlyList<Clip> _visibleClips;
    private Clip? _selectedClip;

    public MainWindowViewModel()
        : this([])
    {
    }

    public MainWindowViewModel(IReadOnlyList<Clip> clips)
    {
        ArgumentNullException.ThrowIfNull(clips);

        _allClips = clips;
        _visibleClips = clips;
        _selectedClip = clips.FirstOrDefault();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Query
    {
        get => _query;
        set
        {
            if (_query == value)
            {
                return;
            }

            _query = value ?? string.Empty;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

    public IReadOnlyList<Clip> VisibleClips
    {
        get => _visibleClips;
        private set
        {
            _visibleClips = value;
            OnPropertyChanged();
        }
    }

    public Clip? SelectedClip
    {
        get => _selectedClip;
        set
        {
            if (_selectedClip == value)
            {
                return;
            }

            _selectedClip = value;
            OnPropertyChanged();
        }
    }

    public void MoveSelectionDown() => MoveSelection(1);

    public void MoveSelectionUp() => MoveSelection(-1);

    public void Reset()
    {
        if (_query.Length > 0)
        {
            _query = string.Empty;
            OnPropertyChanged(nameof(Query));
        }

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        VisibleClips = string.IsNullOrEmpty(Query)
            ? _allClips
            : _allClips.Where(clip =>
                clip.Label.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                clip.Content.Contains(Query, StringComparison.OrdinalIgnoreCase)).ToArray();

        SelectedClip = VisibleClips.FirstOrDefault();
    }

    private void MoveSelection(int offset)
    {
        if (VisibleClips.Count == 0)
        {
            return;
        }

        var currentIndex = 0;
        if (SelectedClip is not null)
        {
            for (var index = 0; index < VisibleClips.Count; index++)
            {
                if (VisibleClips[index] == SelectedClip)
                {
                    currentIndex = index;
                    break;
                }
            }
        }

        var nextIndex = Math.Clamp(currentIndex + offset, 0, VisibleClips.Count - 1);
        SelectedClip = VisibleClips[nextIndex];
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
