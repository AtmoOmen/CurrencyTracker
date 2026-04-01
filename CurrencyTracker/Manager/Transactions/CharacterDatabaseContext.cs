using System.IO;
using Microsoft.Data.Sqlite;

namespace CurrencyTracker.Manager.Transactions;

internal static class CharacterDatabaseContext
{
    public const string DatabaseFileName = "data.db";

    public static string GetDatabasePath(string dataFolder) =>
        Path.Combine(dataFolder, DatabaseFileName);

    public static SqliteConnection OpenConnection(string dataFolder)
    {
        Directory.CreateDirectory(dataFolder);

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = GetDatabasePath(dataFolder),
            Mode       = SqliteOpenMode.ReadWriteCreate,
            Cache      = SqliteCacheMode.Default,
            Pooling    = true
        }.ToString();

        var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            PRAGMA journal_mode = DELETE;
            PRAGMA synchronous = NORMAL;
            PRAGMA temp_store = MEMORY;
            """;
        command.ExecuteNonQuery();

        return connection;
    }
}
