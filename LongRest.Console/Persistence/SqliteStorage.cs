using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace LongRest.ConsoleApp;

public sealed class SqliteStorage
{
    private readonly string connectionString;
    public string DatabasePath { get; }
    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LongRest", "longrest.db");

    public SqliteStorage(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        DatabasePath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
        connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath, Mode = SqliteOpenMode.ReadWriteCreate, ForeignKeys = true, Pooling = false
        }.ToString();
        using var connection = Open();
        using var version = connection.CreateCommand();
        version.CommandText = "PRAGMA user_version;";
        var schemaVersion = Convert.ToInt32(version.ExecuteScalar());
        if (schemaVersion != 0 && schemaVersion != 1) throw new InvalidDataException("This database version is not supported.");
        using var transaction = connection.BeginTransaction();
        using var schema = connection.CreateCommand();
        schema.Transaction = transaction;
        schema.CommandText = """
            CREATE TABLE IF NOT EXISTS Users (
                NormalizedName TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                PasswordSalt BLOB NOT NULL CHECK(length(PasswordSalt) = 16),
                PasswordHash BLOB NOT NULL CHECK(length(PasswordHash) = 32),
                Characters TEXT NOT NULL
            );
            PRAGMA user_version = 1;
            """;
        schema.ExecuteNonQuery();
        transaction.Commit();
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(connectionString);
        try { connection.Open(); return connection; }
        catch { connection.Dispose(); throw; }
    }

    internal List<User> LoadUsers()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Name, PasswordSalt, PasswordHash, Characters FROM Users ORDER BY rowid;";
        using var reader = command.ExecuteReader();
        var users = new List<User>();
        while (reader.Read())
        {
            try
            {
                var user = new User(reader.GetString(0), (byte[])reader[1], (byte[])reader[2]);
                var data = JsonSerializer.Deserialize<List<CharacterData>>(reader.GetString(3)) ??
                    throw new InvalidDataException("Missing saved character data.");
                foreach (var character in CharacterSnapshots.Restore(data)) user.AddCharacter(character);
                users.Add(user);
            }
            catch (Exception exception) when (exception is JsonException or ArgumentException or InvalidOperationException or NullReferenceException)
            {
                throw new InvalidDataException("Could not load saved characters. The database has not been reset.", exception);
            }
        }
        return users;
    }

    internal void SaveUsers(IReadOnlyList<User> users)
    {
        // shortcut: character edits assume one running console; add concurrency checks for simultaneous sessions.
        var snapshots = new List<(User User, string Characters)>();
        foreach (var user in users)
        {
            var characters = user.Characters.Select(CharacterSnapshots.Capture).ToList();
            var ids = characters.Select(character => character.Id).ToHashSet();
            if (characters.SelectMany(character => character.Conditions).Any(condition =>
                    condition.SourceId is { } source && !ids.Contains(source)))
                throw new InvalidDataException("A condition source must belong to the same saved account.");
            snapshots.Add((user, JsonSerializer.Serialize(characters)));
        }
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        foreach (var (user, characters) in snapshots)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO Users (NormalizedName, Name, PasswordSalt, PasswordHash, Characters)
                VALUES ($key, $name, $salt, $hash, $characters)
                ON CONFLICT(NormalizedName) DO UPDATE SET Characters = excluded.Characters
                WHERE Users.PasswordSalt = excluded.PasswordSalt AND Users.PasswordHash = excluded.PasswordHash;
                """;
            command.Parameters.AddWithValue("$key", user.Name.ToUpperInvariant());
            command.Parameters.AddWithValue("$name", user.Name);
            command.Parameters.AddWithValue("$salt", user.PasswordSalt);
            command.Parameters.AddWithValue("$hash", user.PasswordHash);
            command.Parameters.AddWithValue("$characters", characters);
            if (command.ExecuteNonQuery() != 1)
                throw new InvalidOperationException("Another account with that name was saved by a different application instance. Restart and login.");
        }
        transaction.Commit();
    }
}

