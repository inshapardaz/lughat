namespace Lughat.Engine.Api.Search;

/// <summary>
/// Reduces one lowercase token to its stem ("running" → "run") so search can match across
/// inflected forms (spec §6/§8, issue #60). Implementations are pure and per-language — the
/// architecture point of this interface is that adding a new language's stemmer means adding
/// a class and a <see cref="StemmerRegistry"/> registration, not touching IndexingService or
/// SearchService.
/// </summary>
public interface IStemmer
{
    /// <summary>ISO 639-1 language code this stemmer handles (e.g. "en").</summary>
    string LanguageCode { get; }

    /// <summary>Stems one already-lowercased token. Returns the token unchanged if it has no reduction.</summary>
    string Stem(string token);
}

/// <summary>Identity stemmer for languages without a dedicated implementation yet.</summary>
public sealed class NoOpStemmer(string languageCode) : IStemmer
{
    public string LanguageCode { get; } = languageCode;

    public string Stem(string token) => token;
}

/// <summary>
/// Looks up the right <see cref="IStemmer"/> for a dictionary's language, falling back to a
/// no-op stemmer for languages nothing is registered for yet.
/// </summary>
public sealed class StemmerRegistry
{
    private readonly Dictionary<string, IStemmer> _byLanguage;

    public StemmerRegistry(IEnumerable<IStemmer> stemmers)
    {
        _byLanguage = stemmers.ToDictionary(s => s.LanguageCode, StringComparer.OrdinalIgnoreCase);
    }

    public static StemmerRegistry Default { get; } = new([new EnglishPorterStemmer()]);

    public IStemmer Get(string? languageCode) =>
        languageCode is not null && _byLanguage.TryGetValue(languageCode, out var stemmer)
            ? stemmer
            : new NoOpStemmer(languageCode ?? "und");

    /// <summary>Every registered stemmer — used to build a query-time OR across all known languages' stems.</summary>
    public IReadOnlyCollection<IStemmer> All => _byLanguage.Values;
}
