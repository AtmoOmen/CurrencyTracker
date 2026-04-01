using System;
using System.Collections.Generic;
using CurrencyTracker.Infos;
using Microsoft.Data.Sqlite;

namespace CurrencyTracker.Manager.Transactions;

internal static class TransactionStore
{
    private const int CurrentSchemaVersion = 1;

    public static string GetDatabasePath(string dataFolder) =>
        CharacterDatabaseContext.GetDatabasePath(dataFolder);

    public static void EnsureDatabaseReady(string dataFolder, ulong characterContentId)
    {
        if (string.IsNullOrWhiteSpace(dataFolder) || characterContentId == 0)
            return;

        using var connection = OpenReadyConnection(dataFolder, characterContentId);
    }

    public static List<Transaction> LoadTransactions
    (
        string                  dataFolder,
        ulong                   characterContentId,
        uint                    currencyId,
        TransactionFileCategory category,
        ulong                   categoryOwnerId
    )
    {
        using var connection = OpenReadyConnection(dataFolder, characterContentId);
        using var command    = connection.CreateCommand();

        command.CommandText =
            """
            SELECT TimestampTicks, Amount, Change, LocationName, Note
            FROM Transactions
            WHERE CharacterContentId = $characterContentId
              AND CurrencyId = $currencyId
              AND Category = $category
              AND CategoryOwnerId = $categoryOwnerId
            ORDER BY TimestampTicks, Id;
            """;
        command.Parameters.AddWithValue("$characterContentId", (long)characterContentId);
        command.Parameters.AddWithValue("$currencyId",         (long)currencyId);
        command.Parameters.AddWithValue("$category",           (int)category);
        command.Parameters.AddWithValue("$categoryOwnerId",    (long)categoryOwnerId);

        var transactions = new List<Transaction>();
        using var reader = command.ExecuteReader();

        while (reader.Read())
            transactions.Add(ReadTransaction(reader));

        return transactions;
    }

    public static Transaction? LoadLatestTransaction
    (
        string                  dataFolder,
        ulong                   characterContentId,
        uint                    currencyId,
        TransactionFileCategory category,
        ulong                   categoryOwnerId
    )
    {
        using var connection = OpenReadyConnection(dataFolder, characterContentId);
        using var command    = connection.CreateCommand();

        command.CommandText =
            """
            SELECT TimestampTicks, Amount, Change, LocationName, Note
            FROM Transactions
            WHERE CharacterContentId = $characterContentId
              AND CurrencyId = $currencyId
              AND Category = $category
              AND CategoryOwnerId = $categoryOwnerId
            ORDER BY TimestampTicks DESC, Id DESC
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$characterContentId", (long)characterContentId);
        command.Parameters.AddWithValue("$currencyId",         (long)currencyId);
        command.Parameters.AddWithValue("$category",           (int)category);
        command.Parameters.AddWithValue("$categoryOwnerId",    (long)categoryOwnerId);

        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadTransaction(reader) : null;
    }

    public static void InsertTransaction
    (
        string                  dataFolder,
        ulong                   characterContentId,
        uint                    currencyId,
        TransactionFileCategory category,
        ulong                   categoryOwnerId,
        Transaction             transaction
    )
    {
        using var connection          = OpenReadyConnection(dataFolder, characterContentId);
        using var sqliteTransaction   = connection.BeginTransaction();
        InsertTransactions
        (
            connection,
            sqliteTransaction,
            characterContentId,
            currencyId,
            category,
            categoryOwnerId,
            [transaction]
        );
        sqliteTransaction.Commit();
    }

    public static void ReplaceTransactions
    (
        string                  dataFolder,
        ulong                   characterContentId,
        uint                    currencyId,
        TransactionFileCategory category,
        ulong                   categoryOwnerId,
        IReadOnlyCollection<Transaction> transactions
    )
    {
        using var connection        = OpenReadyConnection(dataFolder, characterContentId);
        using var sqliteTransaction = connection.BeginTransaction();

        using (var deleteCommand = connection.CreateCommand())
        {
            deleteCommand.Transaction = sqliteTransaction;
            deleteCommand.CommandText =
                """
                DELETE FROM Transactions
                WHERE CharacterContentId = $characterContentId
                  AND CurrencyId = $currencyId
                  AND Category = $category
                  AND CategoryOwnerId = $categoryOwnerId;
                """;
            deleteCommand.Parameters.AddWithValue("$characterContentId", (long)characterContentId);
            deleteCommand.Parameters.AddWithValue("$currencyId",         (long)currencyId);
            deleteCommand.Parameters.AddWithValue("$category",           (int)category);
            deleteCommand.Parameters.AddWithValue("$categoryOwnerId",    (long)categoryOwnerId);
            deleteCommand.ExecuteNonQuery();
        }

        InsertTransactions
        (
            connection,
            sqliteTransaction,
            characterContentId,
            currencyId,
            category,
            categoryOwnerId,
            transactions
        );

        sqliteTransaction.Commit();
    }

    internal static SqliteConnection OpenReadyConnection(string dataFolder, ulong characterContentId)
    {
        var connection = CharacterDatabaseContext.OpenConnection(dataFolder);

        EnsureSchema(connection);
        TransactionMigrationService.MigrateLegacyFiles(connection, dataFolder, characterContentId);

        return connection;
    }

    internal static void InsertTransactions
    (
        SqliteConnection                connection,
        SqliteTransaction               sqliteTransaction,
        ulong                           characterContentId,
        uint                            currencyId,
        TransactionFileCategory         category,
        ulong                           categoryOwnerId,
        IEnumerable<Transaction>        transactions,
        string?                         legacySourceKey = null
    )
    {
        using var command = connection.CreateCommand();
        command.Transaction = sqliteTransaction;
        command.CommandText =
            """
            INSERT INTO Transactions
            (
                CharacterContentId,
                CurrencyId,
                Category,
                CategoryOwnerId,
                TimestampTicks,
                Amount,
                Change,
                LocationName,
                Note,
                LegacySourceKey
            )
            VALUES
            (
                $characterContentId,
                $currencyId,
                $category,
                $categoryOwnerId,
                $timestampTicks,
                $amount,
                $change,
                $locationName,
                $note,
                $legacySourceKey
            );
            """;

        command.Parameters.Add("$characterContentId", SqliteType.Integer);
        command.Parameters.Add("$currencyId",         SqliteType.Integer);
        command.Parameters.Add("$category",           SqliteType.Integer);
        command.Parameters.Add("$categoryOwnerId",    SqliteType.Integer);
        command.Parameters.Add("$timestampTicks",     SqliteType.Integer);
        command.Parameters.Add("$amount",             SqliteType.Integer);
        command.Parameters.Add("$change",             SqliteType.Integer);
        command.Parameters.Add("$locationName",       SqliteType.Text);
        command.Parameters.Add("$note",               SqliteType.Text);
        command.Parameters.Add("$legacySourceKey",    SqliteType.Text);

        foreach (var transaction in transactions)
        {
            command.Parameters["$characterContentId"].Value = (long)characterContentId;
            command.Parameters["$currencyId"].Value         = (long)currencyId;
            command.Parameters["$category"].Value           = (int)category;
            command.Parameters["$categoryOwnerId"].Value    = (long)categoryOwnerId;
            command.Parameters["$timestampTicks"].Value     = transaction.TimeStamp.Ticks;
            command.Parameters["$amount"].Value             = transaction.Amount;
            command.Parameters["$change"].Value             = transaction.Change;
            command.Parameters["$locationName"].Value       = transaction.LocationName;
            command.Parameters["$note"].Value               = transaction.Note;
            command.Parameters["$legacySourceKey"].Value    = legacySourceKey ?? (object)DBNull.Value;
            command.ExecuteNonQuery();
        }
    }

    private static void EnsureSchema(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS PluginMetadata
            (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS MigrationHistory
            (
                SourceFileKey TEXT PRIMARY KEY,
                SourceFilePath TEXT NOT NULL,
                LastWriteUtcTicks INTEGER NOT NULL,
                ImportedRowCount INTEGER NOT NULL,
                MigratedAtUtcTicks INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Transactions
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CharacterContentId INTEGER NOT NULL,
                CurrencyId INTEGER NOT NULL,
                Category INTEGER NOT NULL,
                CategoryOwnerId INTEGER NOT NULL,
                TimestampTicks INTEGER NOT NULL,
                Amount INTEGER NOT NULL,
                Change INTEGER NOT NULL,
                LocationName TEXT NOT NULL,
                Note TEXT NOT NULL,
                LegacySourceKey TEXT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_Transactions_Query
            ON Transactions (CurrencyId, Category, CategoryOwnerId, TimestampTicks, Id);

            CREATE INDEX IF NOT EXISTS IX_Transactions_Latest
            ON Transactions (CurrencyId, Category, CategoryOwnerId, Id DESC);

            INSERT INTO PluginMetadata(Key, Value)
            VALUES ('SchemaVersion', $schemaVersion)
            ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;
            """;
        command.Parameters.AddWithValue("$schemaVersion", CurrentSchemaVersion.ToString());
        command.ExecuteNonQuery();
    }

    private static Transaction ReadTransaction(SqliteDataReader reader) =>
        new()
        {
            TimeStamp    = new DateTime(reader.GetInt64(0)),
            Amount       = reader.GetInt64(1),
            Change       = reader.GetInt64(2),
            LocationName = reader.GetString(3),
            Note         = reader.GetString(4)
        };
}
