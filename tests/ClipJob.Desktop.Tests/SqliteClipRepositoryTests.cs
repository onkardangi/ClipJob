using Microsoft.Data.Sqlite;
using ClipJob.Desktop;
using Xunit;

namespace ClipJob.Desktop.Tests;

public sealed class SqliteClipRepositoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"clipjob-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task InitializeCreatesSchemaAndSeedsEmptyDatabaseOnce()
    {
        var databasePath = Path.Combine(_directory, "nested", "clipjob.db");
        var repository = new SqliteClipRepository(databasePath);

        await repository.InitializeAsync();
        Assert.True(File.Exists(databasePath));
        Assert.True(await ClipTableExistsAsync(databasePath));
        Assert.Equal(3, (await repository.GetAllAsync()).Count);

        await repository.InitializeAsync();
        Assert.Equal(3, (await repository.GetAllAsync()).Count);
    }

    [Fact]
    public async Task GetAllReturnsPersistedMultilineUnicodeAfterReopen()
    {
        var databasePath = GetDatabasePath();
        var repository = new SqliteClipRepository(databasePath);
        await repository.InitializeAsync();

        const string content = "Résumé — C# / .NET & AWS\n“Distributed systems” 🚀";
        var id = Guid.NewGuid();
        await InsertClipAsync(databasePath, id, "unicode", content);

        var reopenedRepository = new SqliteClipRepository(databasePath);
        var clip = Assert.Single(
            await reopenedRepository.GetAllAsync(),
            candidate => candidate.Id == id);

        Assert.Equal("unicode", clip.Label);
        Assert.Equal(content, clip.Content);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private string GetDatabasePath()
    {
        Directory.CreateDirectory(_directory);
        return Path.Combine(_directory, "clipjob.db");
    }

    private static async Task<bool> ClipTableExistsAsync(string databasePath)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'Clip';";
        return (long)(await command.ExecuteScalarAsync())! == 1;
    }

    private static async Task InsertClipAsync(string databasePath, Guid id, string label, string content)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO Clip (Id, Label, Content) VALUES ($id, $label, $content);";
        command.Parameters.AddWithValue("$id", id.ToString());
        command.Parameters.AddWithValue("$label", label);
        command.Parameters.AddWithValue("$content", content);
        await command.ExecuteNonQueryAsync();
    }
}
