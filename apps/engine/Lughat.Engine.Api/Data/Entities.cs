namespace Lughat.Engine.Api.Data;

// Plain mutable classes rather than records: EF Core tracks and updates entities by
// mutating property values on the tracked instance, which wants settable properties.
// These double as the API's JSON response shapes (see AppJsonContext) — property names
// are the on-the-wire contract, not just the DB column names.

public sealed class DictionaryEntity
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Format { get; set; } = default!;
    public string FilePath { get; set; } = default!;
    public string ContentHash { get; set; } = default!;
    public bool Enabled { get; set; }
    public string? GroupId { get; set; }
    public int SortOrder { get; set; }
    public string? IndexedAt { get; set; }

    // ISO 639-1 code driving which IStemmer BuildIndex() uses (see StemmerRegistry) — defaults
    // to "en" rather than null so search stemming (spec §6/§8, issue #60) works out of the box
    // for the common case without requiring every import to explicitly tag a language.
    public string Language { get; set; } = "en";
}

public sealed class GroupEntity
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int SortOrder { get; set; }
}

public sealed class HistoryEntity
{
    public string Id { get; set; } = default!;
    public string Term { get; set; } = default!;
    public string DictionaryId { get; set; } = default!;
    public string Timestamp { get; set; } = default!;
}

public sealed class FavoriteEntity
{
    public string Id { get; set; } = default!;
    public string Term { get; set; } = default!;
    public string DictionaryId { get; set; } = default!;
    public string? Tag { get; set; }
    public string CreatedAt { get; set; } = default!;
}

public sealed class SettingEntity
{
    public string Key { get; set; } = default!;
    public string ValueJson { get; set; } = default!;
}
