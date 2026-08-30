using Lughat.Engine.Api.Data;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Lughat.Engine.Api.Search;

public sealed record SearchHit(string DictionaryId, string DictionaryName, string Headword, string ArticleHtml, float Score);

/// <summary>
/// Queries across every enabled dictionary's on-disk index at once via a Lucene
/// <see cref="MultiReader"/>. Ranking follows spec §8: exact/prefix headword matches are a
/// different query mode than full-text, so the caller (and eventually the UI) picks the
/// mode rather than the service silently mixing relevance scores across mode types.
/// </summary>
public sealed class SearchService(IndexingService indexingService, DictionaryRepository dictionaryRepository)
{
    private static readonly LuceneVersion Version = LuceneVersion.LUCENE_48;

    public IReadOnlyList<SearchHit> Search(IReadOnlyList<string>? dictionaryIds, string query, string mode, int limit = 20)
    {
        var normalized = query.Trim();
        if (normalized.Length == 0)
        {
            return [];
        }

        var candidates = dictionaryRepository.List().Where(d => d.Enabled);
        if (dictionaryIds is { Count: > 0 })
        {
            var wanted = dictionaryIds.ToHashSet();
            candidates = candidates.Where(d => wanted.Contains(d.Id));
        }

        var dictById = candidates.ToDictionary(d => d.Id);
        var readers = new List<IndexReader>();

        foreach (var dict in dictById.Values)
        {
            var dir = indexingService.IndexDirectoryFor(dict.ContentHash);
            if (System.IO.Directory.Exists(dir) && DirectoryReader.IndexExists(FSDirectory.Open(dir)))
            {
                readers.Add(DirectoryReader.Open(FSDirectory.Open(dir)));
            }
        }

        if (readers.Count == 0)
        {
            return [];
        }

        using var multiReader = new MultiReader(readers.ToArray(), closeSubReaders: true);
        var searcher = new IndexSearcher(multiReader);

        var luceneQuery = BuildQuery(normalized, mode);
        var hits = searcher.Search(luceneQuery, limit).ScoreDocs;

        var results = new List<SearchHit>(hits.Length);
        foreach (var hit in hits)
        {
            var doc = searcher.Doc(hit.Doc);
            var dictId = doc.Get("dictId");
            var dictName = dictById.TryGetValue(dictId, out var d) ? d.Name : dictId;
            results.Add(new SearchHit(dictId, dictName, doc.Get("headword"), doc.Get("articleHtml"), hit.Score));
        }

        return results;
    }

    private static Query BuildQuery(string normalized, string mode)
    {
        var lower = normalized.ToLowerInvariant();
        return mode switch
        {
            "prefix" => new PrefixQuery(new Term("headwordExact", lower)),
            "fuzzy" => new FuzzyQuery(new Term("headwordExact", lower), 2),
            "fulltext" => ParseFullTextQuery(normalized),
            _ => new TermQuery(new Term("headwordExact", lower)),
        };
    }

    private static Query ParseFullTextQuery(string normalized)
    {
        var parser = new QueryParser(Version, "article", new StandardAnalyzer(Version));
        Query baseQuery;
        try
        {
            baseQuery = parser.Parse(QueryParserBase.Escape(normalized));
        }
        catch (ParseException)
        {
            baseQuery = new TermQuery(new Term("article", normalized.ToLowerInvariant()));
        }

        // OR in every registered language's stemmed reading of the query (issue #60) — e.g.
        // "running" also matches articles indexed under the stem "run" — without touching this
        // method's signature when a new IStemmer is registered; StemmerRegistry.Default.All
        // just grows.
        var stemmedQuery = BuildStemmedQuery(normalized);
        if (stemmedQuery is null)
        {
            return baseQuery;
        }

        return new BooleanQuery { { baseQuery, Occur.SHOULD }, { stemmedQuery, Occur.SHOULD } };
    }

    private static Query? BuildStemmedQuery(string normalized)
    {
        var terms = normalized.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (terms.Length == 0)
        {
            return null;
        }

        var query = new BooleanQuery();
        foreach (var term in terms)
        {
            foreach (var stemmer in StemmerRegistry.Default.All)
            {
                var stemmed = stemmer.Stem(term);
                query.Add(new TermQuery(new Term("articleStemmed", stemmed)), Occur.SHOULD);
                query.Add(new TermQuery(new Term("headwordStemmed", stemmed)), Occur.SHOULD);
            }
        }

        return query;
    }
}
