using Lughat.Engine.Api.Formats;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Lughat.Engine.Api.Search;

/// <summary>
/// Spike-quality search index (issue: "Spike: minimal Lucene.NET index + exact/prefix lookup endpoint").
/// In-memory (RAMDirectory) only — the production pipeline (Phase 1) persists a content-hash keyed
/// index on disk with incremental rebuilds, per spec §7 / §8.
/// </summary>
public sealed class LuceneIndexService
{
    private const LuceneVersion Version = LuceneVersion.LUCENE_48;

    private readonly Lucene.Net.Store.Directory _directory = new RAMDirectory();
    private readonly Dictionary<string, string> _articlesByHeadword = new(StringComparer.OrdinalIgnoreCase);

    public void Build(IEnumerable<StarDictEntry> entries)
    {
        var analyzer = new StandardAnalyzer(Version);
        var config = new IndexWriterConfig(Version, analyzer);
        using var writer = new IndexWriter(_directory, config);

        foreach (var entry in entries)
        {
            var doc = new Document
            {
                new StringField("headword_exact", entry.Headword.ToLowerInvariant(), Field.Store.YES),
                new StringField("headword", entry.Headword, Field.Store.YES),
            };
            writer.AddDocument(doc);
            _articlesByHeadword[entry.Headword] = entry.Article;
        }

        writer.Commit();
    }

    public IReadOnlyList<LookupResult> Lookup(string term, string mode)
    {
        using var reader = DirectoryReader.Open(_directory);
        var searcher = new IndexSearcher(reader);

        var normalized = term.ToLowerInvariant();
        Query query = mode == "prefix"
            ? new PrefixQuery(new Term("headword_exact", normalized))
            : new TermQuery(new Term("headword_exact", normalized));

        var hits = searcher.Search(query, 20).ScoreDocs;
        var results = new List<LookupResult>(hits.Length);
        foreach (var hit in hits)
        {
            var doc = searcher.Doc(hit.Doc);
            var headword = doc.Get("headword");
            results.Add(new LookupResult(headword, _articlesByHeadword.GetValueOrDefault(headword, string.Empty)));
        }

        return results;
    }
}

public sealed record LookupResult(string Headword, string Article);
