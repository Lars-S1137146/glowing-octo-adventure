using LongRest.ConsoleApp;
using Microsoft.Data.Sqlite;

namespace LongRest.Domain.Tests.Persistence;

internal sealed class TemporaryDatabase : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "LongRest-tests-" + Guid.NewGuid().ToString("N"));
    // Semicolons also exercise connection-string escaping.
    public string PathName => Path.Combine(directory, "local;characters.db");
    public UserAccounts Open() => new(new SqliteStorage(PathName));

    public object? Execute(string sql)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = PathName, Pooling = false }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar();
    }

    public void Dispose()
    {
        if (!Directory.Exists(directory)) return;
        foreach (var file in Directory.GetFiles(directory)) File.Delete(file);
        Directory.Delete(directory);
    }
}
