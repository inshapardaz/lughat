namespace Lughat.Engine.Api.Data;

public sealed record DictionaryRecord(
    string Id,
    string Name,
    string Format,
    string FilePath,
    string ContentHash,
    bool Enabled,
    string? GroupId,
    int SortOrder,
    string? IndexedAt);

public sealed record GroupRecord(string Id, string Name, int SortOrder);

public sealed record HistoryRecord(string Id, string Term, string DictionaryId, string Timestamp);

public sealed record FavoriteRecord(string Id, string Term, string DictionaryId, string? Tag, string CreatedAt);
