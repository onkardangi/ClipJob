using System.Text.Json;

namespace ClipJob.Desktop;

public sealed class AppSettings
{
    public GlobalShortcut Shortcut { get; set; } = GlobalShortcut.Default;
}

public sealed class AppSettingsStore
{
    private readonly string _path;

    public AppSettingsStore(string path)
    {
        _path = path;
    }

    public static AppSettingsStore CreateDefault()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClipJob");
        return new AppSettingsStore(Path.Combine(directory, "settings.json"));
    }

    public async Task<AppSettings> LoadAsync()
    {
        if (!File.Exists(_path))
        {
            return new AppSettings();
        }

        await using var stream = File.OpenRead(_path);
        return await JsonSerializer.DeserializeAsync<AppSettings>(stream) ?? new AppSettings();
    }

    public async Task SaveAsync(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(_path)!;
        Directory.CreateDirectory(directory);
        await using var stream = File.Create(_path);
        await JsonSerializer.SerializeAsync(stream, settings, new JsonSerializerOptions { WriteIndented = true });
    }
}
