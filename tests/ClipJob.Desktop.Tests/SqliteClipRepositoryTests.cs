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

    [Fact]
    public async Task AddPersistsMultilineUnicodeAfterReopen()
    {
        var databasePath = GetDatabasePath();
        var repository = new SqliteClipRepository(databasePath);
        await repository.InitializeAsync();
        var clip = new Clip(Guid.NewGuid(), "unicode", "Résumé 🚀\nSecond line");

        await repository.AddAsync(clip);

        var reopened = new SqliteClipRepository(databasePath);
        Assert.Equal(clip, Assert.Single(await reopened.GetAllAsync(), item => item.Id == clip.Id));
    }

    [Fact]
    public async Task UpdatePersistsValuesAndIdAfterReopen()
    {
        var databasePath = GetDatabasePath();
        var repository = new SqliteClipRepository(databasePath);
        await repository.InitializeAsync();
        var id = Guid.NewGuid();
        await repository.AddAsync(new Clip(id, "role", "Engineer"));

        await repository.UpdateAsync(new Clip(id, "target-role", "Senior Engineer"));

        var reopened = new SqliteClipRepository(databasePath);
        var clip = Assert.Single(await reopened.GetAllAsync(), item => item.Id == id);
        Assert.Equal("target-role", clip.Label);
        Assert.Equal("Senior Engineer", clip.Content);
    }

    [Fact]
    public async Task DeletePersistsAfterReopen()
    {
        var databasePath = GetDatabasePath();
        var repository = new SqliteClipRepository(databasePath);
        await repository.InitializeAsync();
        var clip = new Clip(Guid.NewGuid(), "temporary", "Delete me");
        await repository.AddAsync(clip);

        await repository.DeleteAsync(clip.Id);

        var reopened = new SqliteClipRepository(databasePath);
        Assert.DoesNotContain(await reopened.GetAllAsync(), item => item.Id == clip.Id);
    }

    [Fact]
    public async Task DuplicateLabelIsRejectedCaseInsensitivelyWithoutChangingData()
    {
        var databasePath = GetDatabasePath();
        var repository = new SqliteClipRepository(databasePath);
        await repository.InitializeAsync();
        var before = await repository.GetAllAsync();

        await Assert.ThrowsAsync<SqliteException>(() =>
            repository.AddAsync(new Clip(Guid.NewGuid(), "EMAIL", "Other")));

        Assert.Equal(before, await repository.GetAllAsync());
    }

    [Fact]
    public async Task InitializeReportsExistingDuplicateLabelsWithoutDeletingData()
    {
        var databasePath = GetDatabasePath();
        await CreateLegacyDatabaseWithDuplicatesAsync(databasePath);
        var repository = new SqliteClipRepository(databasePath);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(repository.InitializeAsync);

        Assert.Contains("duplicate labels", exception.Message);
        Assert.Equal(2, await CountClipsAsync(databasePath));
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

    private static async Task CreateLegacyDatabaseWithDuplicatesAsync(string databasePath)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE Clip (Id TEXT PRIMARY KEY NOT NULL, Label TEXT NOT NULL, Content TEXT NOT NULL);
            INSERT INTO Clip VALUES ('1', 'email', 'first');
            INSERT INTO Clip VALUES ('2', 'EMAIL', 'second');
            """;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<long> CountClipsAsync(string databasePath)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Clip;";
        return (long)(await command.ExecuteScalarAsync())!;
    }
}
