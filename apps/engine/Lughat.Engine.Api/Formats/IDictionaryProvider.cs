namespace Lughat.Engine.Api.Formats;

/// <summary>
/// One normalized entry parsed out of a source dictionary file, regardless of format.
/// </summary>
public sealed record DictionaryEntry(string Headword, string ArticleHtml);

/// <summary>
/// A parser for one dictionary file format. Implementations never touch the search index,
/// SQLite, or HTTP — they only turn a file on disk into a stream of <see cref="DictionaryEntry"/>.
/// See spec §5.
/// </summary>
public interface IDictionaryProvider
{
    /// <summary>Stable identifier stored in the Dictionaries table's Format column (e.g. "stardict").</summary>
    string FormatId { get; }

    /// <summary>Whether this provider can open the given path (checked by extension/sidecar files).</summary>
    bool CanOpen(string path);

    /// <summary>Streams every entry in the dictionary. May throw <see cref="DictionaryFormatException"/>.</summary>
    IEnumerable<DictionaryEntry> ReadEntries(string path);
}

/// <summary>
/// Raised by a provider when a file can't be read — carries a stable error code (spec §9's
/// localisation boundary: the engine never returns human-readable text, only codes the
/// renderer maps to a localized message).
/// </summary>
public sealed class DictionaryFormatException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}

/// <summary>
/// Every registered format provider, looked up by format id or by which one claims a path.
/// New formats plug in here without touching indexing, search, or the API layer.
/// </summary>
public sealed class DictionaryProviderRegistry
{
    private readonly List<IDictionaryProvider> _providers = [];

    public DictionaryProviderRegistry Register(IDictionaryProvider provider)
    {
        _providers.Add(provider);
        return this;
    }

    public IReadOnlyList<IDictionaryProvider> Providers => _providers;

    public IDictionaryProvider? FindProviderForPath(string path) =>
        _providers.FirstOrDefault(p => p.CanOpen(path));

    public IDictionaryProvider? FindProviderById(string formatId) =>
        _providers.FirstOrDefault(p => p.FormatId == formatId);
}
