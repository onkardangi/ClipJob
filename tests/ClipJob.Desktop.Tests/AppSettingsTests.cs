using Avalonia.Input;
using Xunit;

namespace ClipJob.Desktop.Tests;

public sealed class AppSettingsTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"clipjob-settings-tests-{Guid.NewGuid():N}");

    [Fact]
    public void DefaultShortcutIsCommandShiftV()
    {
        Assert.Equal(Key.V, GlobalShortcut.Default.Key);
        Assert.Equal(KeyModifiers.Meta | KeyModifiers.Shift, GlobalShortcut.Default.Modifiers);
        Assert.Equal("⇧⌘V", GlobalShortcut.Default.DisplayText);
    }

    [Theory]
    [InlineData(Key.V, KeyModifiers.Meta, true)]
    [InlineData(Key.K, KeyModifiers.Meta | KeyModifiers.Alt, true)]
    [InlineData(Key.K, KeyModifiers.Shift, false)]
    [InlineData(Key.F1, KeyModifiers.Meta, false)]
    public void ShortcutRequiresCommandAndALetter(Key key, KeyModifiers modifiers, bool expected)
    {
        Assert.Equal(expected, new GlobalShortcut(key, modifiers).IsValid);
    }

    [Fact]
    public async Task SavedShortcutCanBeLoadedByANewStoreInstance()
    {
        var path = Path.Combine(_directory, "settings.json");
        var settings = new AppSettings
        {
            Shortcut = new GlobalShortcut(Key.K, KeyModifiers.Meta | KeyModifiers.Alt)
        };

        await new AppSettingsStore(path).SaveAsync(settings);
        var loaded = await new AppSettingsStore(path).LoadAsync();

        Assert.Equal(settings.Shortcut, loaded.Shortcut);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }
}
