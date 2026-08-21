using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ClipJob.Desktop;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly IClipRepository? _repository;
    private readonly List<Clip> _allClips;
    private string _query = string.Empty;
    private IReadOnlyList<Clip> _visibleClips;
    private Clip? _selectedClip;

    public MainWindowViewModel()
        : this([])
    {
    }

    public MainWindowViewModel(IReadOnlyList<Clip> clips)
        : this(clips, null)
    {
    }

    public MainWindowViewModel(IReadOnlyList<Clip> clips, IClipRepository? repository)
    {
        ArgumentNullException.ThrowIfNull(clips);

        _repository = repository;
        _allClips = [.. clips];
        _visibleClips = _allClips;
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
            OnPropertyChanged(nameof(HasNoResults));
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
            OnPropertyChanged(nameof(HasSelection));
        }
    }

    public bool HasSelection => SelectedClip is not null;

    public bool HasNoResults => VisibleClips.Count == 0;

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

    public async Task<string?> AddAsync(string label, string content)
    {
        var validationError = Validate(label, content);
        if (validationError is not null)
        {
            return validationError;
        }

        var clip = new Clip(Guid.NewGuid(), label.Trim(), content);
        try
        {
            await RequireRepository().AddAsync(clip);
        }
        catch (Microsoft.Data.Sqlite.SqliteException exception) when (exception.SqliteErrorCode == 19)
        {
            return "A clip with that label already exists.";
        }

        _allClips.Add(clip);
        ApplyFilter(clip);
        return null;
    }

    public async Task<string?> UpdateAsync(Clip existingClip, string label, string content)
    {
        ArgumentNullException.ThrowIfNull(existingClip);
        var validationError = Validate(label, content, existingClip.Id);
        if (validationError is not null)
        {
            return validationError;
        }

        var updatedClip = existingClip with { Label = label.Trim(), Content = content };
        try
        {
            await RequireRepository().UpdateAsync(updatedClip);
        }
        catch (Microsoft.Data.Sqlite.SqliteException exception) when (exception.SqliteErrorCode == 19)
        {
            return "A clip with that label already exists.";
        }

        var index = _allClips.FindIndex(clip => clip.Id == existingClip.Id);
        if (index < 0)
        {
            throw new InvalidOperationException($"Clip '{existingClip.Id}' is not loaded.");
        }

        _allClips[index] = updatedClip;
        ApplyFilter(updatedClip);
        return null;
    }

    public async Task DeleteAsync(Clip clip)
    {
        ArgumentNullException.ThrowIfNull(clip);
        await RequireRepository().DeleteAsync(clip.Id);
        _allClips.RemoveAll(candidate => candidate.Id == clip.Id);
        ApplyFilter();
    }

    private string? Validate(string label, string content, Guid? existingId = null)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return "Label is required.";
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return "Content is required.";
        }

        return _allClips.Any(clip =>
            clip.Id != existingId &&
            string.Equals(clip.Label, label.Trim(), StringComparison.OrdinalIgnoreCase))
            ? "A clip with that label already exists."
            : null;
    }

    private IClipRepository RequireRepository() =>
        _repository ?? throw new InvalidOperationException("Clip management requires a repository.");

    private void ApplyFilter(Clip? preferredSelection = null)
    {
        VisibleClips = string.IsNullOrEmpty(Query)
            ? _allClips.ToArray()
            : _allClips.Where(clip =>
                clip.Label.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                clip.Content.Contains(Query, StringComparison.OrdinalIgnoreCase)).ToArray();

        SelectedClip = preferredSelection is not null && VisibleClips.Contains(preferredSelection)
            ? preferredSelection
            : VisibleClips.FirstOrDefault();
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
