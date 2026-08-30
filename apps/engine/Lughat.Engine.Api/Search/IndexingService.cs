using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Lughat.Engine.Api.Formats;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Lughat.Engine.Api.Search;

public sealed record IndexProgressEvent(string DictionaryId, int Percent, bool Complete);

/// <summary>
/// Builds and caches a Lucene.NET index per dictionary, keyed by the source file's content
/// hash (spec §7 / §8) — re-adding an unchanged file skips reindexing entirely, and bumping
/// <see cref="SchemaVersion"/> forces every dictionary to rebuild on the next startup
/// instead of silently reading a stale/incompatible index.
/// </summary>
public sealed class IndexingService(string indexRoot)
{
    public const int SchemaVersion = 1;

    private static readonly LuceneVersion Version = LuceneVersion.LUCENE_48;

    public static string ComputeContentHash(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexStringLower(hash);
    }

    public string IndexDirectoryFor(string contentHash) =>
        Path.Combine(indexRoot, $"v{SchemaVersion}", contentHash);

    public bool IsIndexed(string contentHash) =>
        System.IO.Directory.Exists(IndexDirectoryFor(contentHash)) &&
        DirectoryReader.IndexExists(FSDirectory.Open(IndexDirectoryFor(contentHash)));

    public void BuildIndex(
        string dictionaryId,
        string contentHash,
        IEnumerable<DictionaryEntry> entries,
        string? language = null,
        Action<IndexProgressEvent>? onProgress = null)
    {
        var indexDir = IndexDirectoryFor(contentHash);
        System.IO.Directory.CreateDirectory(indexDir);

        using var directory = FSDirectory.Open(indexDir);
        // Stemmed shadow fields (headwordStemmed/articleStemmed) get a per-language analyzer
        // via StemmerRegistry (issue #60) so "running" can match "run" at search time without
        // touching how the plain, unstemmed headword/article fields are analyzed.
        var stemmer = StemmerRegistry.Default.Get(language);
        var stemmingAnalyzer = new StemmingAnalyzer(Version, stemmer);
        var analyzer = new PerFieldAnalyzerWrapper(
            new StandardAnalyzer(Version),
            new Dictionary<string, Analyzer>
            {
                ["headwordStemmed"] = stemmingAnalyzer,
                ["articleStemmed"] = stemmingAnalyzer,
            });
        var config = new IndexWriterConfig(Version, analyzer) { OpenMode = OpenMode.CREATE };
        using var writer = new IndexWriter(directory, config);

        var entryList = entries as IReadOnlyList<DictionaryEntry> ?? entries.ToList();
        var total = Math.Max(entryList.Count, 1);

        for (var i = 0; i < entryList.Count; i++)
        {
            var entry = entryList[i];
            var doc = new Document
            {
                new StringField("dictId", dictionaryId, Field.Store.YES),
                new StringField("headwordExact", entry.Headword.ToLowerInvariant(), Field.Store.NO),
                new TextField("headword", entry.Headword, Field.Store.YES),
                new TextField("article", StripTags(entry.ArticleHtml), Field.Store.NO),
                new TextField("headwordStemmed", entry.Headword, Field.Store.NO),
                new TextField("articleStemmed", StripTags(entry.ArticleHtml), Field.Store.NO),
                new StoredField("articleHtml", entry.ArticleHtml),
            };
            writer.AddDocument(doc);

            if (i % 25 == 0 || i == entryList.Count - 1)
            {
                onProgress?.Invoke(new IndexProgressEvent(dictionaryId, (int)((i + 1) / (double)total * 100), false));
            }
        }

        writer.Commit();
        onProgress?.Invoke(new IndexProgressEvent(dictionaryId, 100, true));
    }

    private static string StripTags(string html) => Regex.Replace(html, "<[^>]+>", " ");
}
