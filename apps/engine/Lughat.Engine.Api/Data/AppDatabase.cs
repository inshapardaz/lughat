using Dapper;
using Microsoft.Data.Sqlite;

namespace Lughat.Engine.Api.Data;

/// <summary>
/// SQLite storage for dictionary metadata, groups, history, favorites, and settings
/// (spec §7). Schema is versioned via SQLite's own <c>PRAGMA user_version</c> — each
/// migration below runs at most once per database, in order, at startup.
/// </summary>
public sealed class AppDatabase
{
    private static readonly string[] Migrations =
    [
        """
        CREATE TABLE Groups (
            Id TEXT PRIMARY KEY,
            Name TEXT NOT NULL,
            SortOrder INTEGER NOT NULL DEFAULT 0
        );

        CREATE TABLE Dictionaries (
            Id TEXT PRIMARY KEY,
            Name TEXT NOT NULL,
            Format TEXT NOT NULL,
            FilePath TEXT NOT NULL,
            ContentHash TEXT NOT NULL,
            Enabled INTEGER NOT NULL DEFAULT 1,
            GroupId TEXT NULL REFERENCES Groups(Id),
            SortOrder INTEGER NOT NULL DEFAULT 0,
            IndexedAt TEXT NULL
        );

        CREATE TABLE History (
            Id TEXT PRIMARY KEY,
            Term TEXT NOT NULL,
            DictionaryId TEXT NOT NULL REFERENCES Dictionaries(Id),
            Timestamp TEXT NOT NULL
        );

        CREATE TABLE Favorites (
            Id TEXT PRIMARY KEY,
            Term TEXT NOT NULL,
            DictionaryId TEXT NOT NULL REFERENCES Dictionaries(Id),
            Tag TEXT NULL,
            CreatedAt TEXT NOT NULL
        );

        CREATE TABLE Settings (
            Key TEXT PRIMARY KEY,
            ValueJson TEXT NOT NULL
        );
        """,
    ];

    private readonly string _connectionString;

    public AppDatabase(string dbPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        // Pooling off: this is a single low-throughput local process, and pooled connections
        // keep a native handle open past Dispose(), which stops the db file from being moved
        // or deleted (e.g. by tests, or a future "reset app data" feature) until the pool is
        // explicitly cleared.
        _connectionString = $"Data Source={dbPath};Pooling=False";
    }

    public void Migrate()
    {
        using var connection = OpenConnection();
        var currentVersion = connection.ExecuteScalar<long>("PRAGMA user_version");

        for (var version = currentVersion; version < Migrations.Length; version++)
        {
            using var transaction = connection.BeginTransaction();
            connection.Execute(Migrations[version], transaction: transaction);
            connection.Execute($"PRAGMA user_version = {version + 1}", transaction: transaction);
            transaction.Commit();
        }
    }

    public SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
