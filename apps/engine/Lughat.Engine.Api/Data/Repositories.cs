using Dapper;

namespace Lughat.Engine.Api.Data;

public sealed class DictionaryRepository(AppDatabase db)
{
    // SQLite has no native boolean type — Microsoft.Data.Sqlite reports the Enabled column
    // as Int64, and Dapper's fast record-constructor materializer requires an exact type
    // match, so it can't bind that straight into `bool Enabled` on DictionaryRecord. This
    // row type mirrors the raw column types; List/Find map it to the public record.
    private sealed record DictionaryRow(
        string Id, string Name, string Format, string FilePath, string ContentHash,
        long Enabled, string? GroupId, long SortOrder, string? IndexedAt)
    {
        public DictionaryRecord ToRecord() => new(Id, Name, Format, FilePath, ContentHash, Enabled != 0, GroupId, (int)SortOrder, IndexedAt);
    }

    public IReadOnlyList<DictionaryRecord> List()
    {
        using var connection = db.OpenConnection();
        return connection.Query<DictionaryRow>(
            "SELECT Id, Name, Format, FilePath, ContentHash, Enabled, GroupId, SortOrder, IndexedAt " +
            "FROM Dictionaries ORDER BY GroupId, SortOrder").Select(row => row.ToRecord()).ToList();
    }

    public DictionaryRecord? Find(string id)
    {
        using var connection = db.OpenConnection();
        return connection.QuerySingleOrDefault<DictionaryRow>(
            "SELECT Id, Name, Format, FilePath, ContentHash, Enabled, GroupId, SortOrder, IndexedAt " +
            "FROM Dictionaries WHERE Id = @id", new { id })?.ToRecord();
    }

    public DictionaryRecord Insert(string name, string format, string filePath, string contentHash)
    {
        using var connection = db.OpenConnection();
        var nextSortOrder = connection.ExecuteScalar<int>(
            "SELECT COALESCE(MAX(SortOrder), -1) + 1 FROM Dictionaries");

        var record = new DictionaryRecord(
            Id: Guid.NewGuid().ToString("n"),
            Name: name,
            Format: format,
            FilePath: filePath,
            ContentHash: contentHash,
            Enabled: true,
            GroupId: null,
            SortOrder: nextSortOrder,
            IndexedAt: null);

        connection.Execute(
            "INSERT INTO Dictionaries (Id, Name, Format, FilePath, ContentHash, Enabled, GroupId, SortOrder, IndexedAt) " +
            "VALUES (@Id, @Name, @Format, @FilePath, @ContentHash, @Enabled, @GroupId, @SortOrder, @IndexedAt)",
            record);

        return record;
    }

    public void MarkIndexed(string id, string indexedAtIso)
    {
        using var connection = db.OpenConnection();
        connection.Execute("UPDATE Dictionaries SET IndexedAt = @indexedAtIso WHERE Id = @id", new { id, indexedAtIso });
    }

    public void SetEnabled(string id, bool enabled)
    {
        using var connection = db.OpenConnection();
        connection.Execute("UPDATE Dictionaries SET Enabled = @enabled WHERE Id = @id", new { id, enabled });
    }

    public void UpdateOrder(string id, string? groupId, int sortOrder)
    {
        using var connection = db.OpenConnection();
        connection.Execute(
            "UPDATE Dictionaries SET GroupId = @groupId, SortOrder = @sortOrder WHERE Id = @id",
            new { id, groupId, sortOrder });
    }

    public bool Delete(string id)
    {
        using var connection = db.OpenConnection();
        var affected = connection.Execute("DELETE FROM Dictionaries WHERE Id = @id", new { id });
        return affected > 0;
    }
}

public sealed class GroupRepository(AppDatabase db)
{
    // Same Int64-vs-int mismatch as DictionaryRow above.
    private sealed record GroupRow(string Id, string Name, long SortOrder)
    {
        public GroupRecord ToRecord() => new(Id, Name, (int)SortOrder);
    }

    public IReadOnlyList<GroupRecord> List()
    {
        using var connection = db.OpenConnection();
        return connection.Query<GroupRow>("SELECT Id, Name, SortOrder FROM Groups ORDER BY SortOrder")
            .Select(row => row.ToRecord()).ToList();
    }

    public GroupRecord Create(string name)
    {
        using var connection = db.OpenConnection();
        var nextSortOrder = connection.ExecuteScalar<int>("SELECT COALESCE(MAX(SortOrder), -1) + 1 FROM Groups");
        var record = new GroupRecord(Guid.NewGuid().ToString("n"), name, nextSortOrder);
        connection.Execute("INSERT INTO Groups (Id, Name, SortOrder) VALUES (@Id, @Name, @SortOrder)", record);
        return record;
    }
}

public sealed class HistoryRepository(AppDatabase db)
{
    public void Record(string term, string dictionaryId, string timestampIso)
    {
        using var connection = db.OpenConnection();
        connection.Execute(
            "INSERT INTO History (Id, Term, DictionaryId, Timestamp) VALUES (@Id, @Term, @DictionaryId, @Timestamp)",
            new HistoryRecord(Guid.NewGuid().ToString("n"), term, dictionaryId, timestampIso));
    }

    public IReadOnlyList<HistoryRecord> Recent(int limit = 50)
    {
        using var connection = db.OpenConnection();
        return connection.Query<HistoryRecord>(
            "SELECT Id, Term, DictionaryId, Timestamp FROM History ORDER BY Timestamp DESC LIMIT @limit",
            new { limit }).AsList();
    }
}

public sealed class FavoriteRepository(AppDatabase db)
{
    public FavoriteRecord Add(string term, string dictionaryId, string? tag, string createdAtIso)
    {
        using var connection = db.OpenConnection();
        var record = new FavoriteRecord(Guid.NewGuid().ToString("n"), term, dictionaryId, tag, createdAtIso);
        connection.Execute(
            "INSERT INTO Favorites (Id, Term, DictionaryId, Tag, CreatedAt) VALUES (@Id, @Term, @DictionaryId, @Tag, @CreatedAt)",
            record);
        return record;
    }

    public IReadOnlyList<FavoriteRecord> List()
    {
        using var connection = db.OpenConnection();
        return connection.Query<FavoriteRecord>(
            "SELECT Id, Term, DictionaryId, Tag, CreatedAt FROM Favorites ORDER BY CreatedAt DESC").AsList();
    }

    public bool Remove(string id)
    {
        using var connection = db.OpenConnection();
        var affected = connection.Execute("DELETE FROM Favorites WHERE Id = @id", new { id });
        return affected > 0;
    }
}

public sealed class SettingsRepository(AppDatabase db)
{
    public string? Get(string key)
    {
        using var connection = db.OpenConnection();
        return connection.QuerySingleOrDefault<string?>(
            "SELECT ValueJson FROM Settings WHERE Key = @key", new { key });
    }

    public void Set(string key, string valueJson)
    {
        using var connection = db.OpenConnection();
        connection.Execute(
            "INSERT INTO Settings (Key, ValueJson) VALUES (@key, @valueJson) " +
            "ON CONFLICT(Key) DO UPDATE SET ValueJson = @valueJson",
            new { key, valueJson });
    }
}
