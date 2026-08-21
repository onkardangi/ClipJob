using Microsoft.Data.Sqlite;

namespace ClipJob.Desktop;

public sealed class SqliteClipRepository(string databasePath) : IClipRepository
{
    private const string DatabaseFileName = "clipjob.db";

    private static readonly IReadOnlyList<Clip> SeedClips =
    [
        new(Guid.Parse("c050aef9-098c-41af-a956-0d69e567232c"), "email", "YOUR_EMAIL"),
        new(Guid.Parse("7df8e85c-92b4-45e4-9060-5f8805579d92"), "linkedin", "YOUR_LINKEDIN_URL"),
        new(Guid.Parse("46eae114-b608-489b-a706-6da162396314"), "experience", "Built high-throughput REST APIs...")
    ];

    private readonly string _databasePath = databasePath;
    private readonly string _connectionString = new SqliteConnectionStringBuilder
    {
        DataSource = databasePath
    }.ToString();

    public static string DefaultDatabasePath
    {
        get
        {
            var applicationData = OperatingSystem.IsMacOS()
                ? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Library",
                    "Application Support")
                : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            return Path.Combine(applicationData, "ClipJob", DatabaseFileName);
        }
    }

    public static SqliteClipRepository CreateDefault()
    {
        return new SqliteClipRepository(DefaultDatabasePath);
    }

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_databasePath))!);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync();
        await CreateSchemaAsync(connection, transaction);
        await SeedIfEmptyAsync(connection, transaction);
        await transaction.CommitAsync();
    }

    public async Task<IReadOnlyList<Clip>> GetAllAsync()
    {
        var clips = new List<Clip>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Label, Content FROM Clip ORDER BY rowid;";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            clips.Add(new Clip(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.GetString(2)));
        }

        return clips;
    }

    public async Task AddAsync(Clip clip)
    {
        ArgumentNullException.ThrowIfNull(clip);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO Clip (Id, Label, Content) VALUES ($id, $label, $content);";
        AddClipParameters(command, clip);
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateAsync(Clip clip)
    {
        ArgumentNullException.ThrowIfNull(clip);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "UPDATE Clip SET Label = $label, Content = $content WHERE Id = $id;";
        AddClipParameters(command, clip);
        if (await command.ExecuteNonQueryAsync() == 0)
        {
            throw new InvalidOperationException($"Clip '{clip.Id}' does not exist.");
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Clip WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id.ToString());
        await command.ExecuteNonQueryAsync();
    }

    private static async Task CreateSchemaAsync(SqliteConnection connection, SqliteTransaction transaction)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS Clip (
                Id TEXT PRIMARY KEY NOT NULL,
                Label TEXT NOT NULL CHECK (length(trim(Label)) > 0),
                Content TEXT NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync();

        command.CommandText =
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_Clip_Label_NoCase ON Clip (Label COLLATE NOCASE);";
        try
        {
            await command.ExecuteNonQueryAsync();
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException(
                "The clip database contains duplicate labels. Rename duplicates before continuing.",
                exception);
        }
    }

    private static void AddClipParameters(SqliteCommand command, Clip clip)
    {
        command.Parameters.AddWithValue("$id", clip.Id.ToString());
        command.Parameters.AddWithValue("$label", clip.Label);
        command.Parameters.AddWithValue("$content", clip.Content);
    }

    private static async Task SeedIfEmptyAsync(SqliteConnection connection, SqliteTransaction transaction)
    {
        await using var countCommand = connection.CreateCommand();
        countCommand.Transaction = transaction;
        countCommand.CommandText = "SELECT COUNT(*) FROM Clip;";
        var count = (long)(await countCommand.ExecuteScalarAsync())!;
        if (count != 0)
        {
            return;
        }

        foreach (var clip in SeedClips)
        {
            await using var insertCommand = connection.CreateCommand();
            insertCommand.Transaction = transaction;
            insertCommand.CommandText = "INSERT INTO Clip (Id, Label, Content) VALUES ($id, $label, $content);";
            insertCommand.Parameters.AddWithValue("$id", clip.Id.ToString());
            insertCommand.Parameters.AddWithValue("$label", clip.Label);
            insertCommand.Parameters.AddWithValue("$content", clip.Content);
            await insertCommand.ExecuteNonQueryAsync();
        }
    }
}
