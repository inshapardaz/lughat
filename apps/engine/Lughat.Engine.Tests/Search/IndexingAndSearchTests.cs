using Lughat.Engine.Api.Data;
using Lughat.Engine.Api.Formats;
using Lughat.Engine.Api.Search;

namespace Lughat.Engine.Tests.Search;

public class IndexingAndSearchTests : IDisposable
{
    private readonly string _tempDir;
    private readonly LughatDbContext _db;
    private readonly DictionaryRepository _dictionaries;
    private readonly IndexingService _indexing;
    private readonly SearchService _search;

    public IndexingAndSearchTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "lughat-tests-" + Guid.NewGuid().ToString("n"));
        _db = TestDb.CreateMigrated(Path.Combine(_tempDir, "db", "app.db"));
        _dictionaries = new DictionaryRepository(_db);
        _indexing = new IndexingService(Path.Combine(_tempDir, "index"));
        _search = new SearchService(_indexing, _dictionaries);
    }

    public void Dispose()
    {
        _db.Dispose();
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup — a lingering file handle shouldn't fail the test run.
        }
    }

    [Fact]
    public void Search_finds_exact_prefix_fuzzy_and_fulltext_matches()
    {
        var record = _dictionaries.Insert("Test Dict", "wordlist", "test.tsv", "hash1");
        _indexing.BuildIndex(record.Id, record.ContentHash,
        [
            new DictionaryEntry("apple", "A round fruit that grows on trees."),
            new DictionaryEntry("application", "A formal request, or a computer program."),
        ]);
        _dictionaries.MarkIndexed(record.Id, DateTimeOffset.UtcNow.ToString("O"));

        var exact = _search.Search(null, "apple", "exact");
        Assert.Single(exact);
        Assert.Equal("apple", exact[0].Headword);

        var prefix = _search.Search(null, "app", "prefix");
        Assert.Equal(2, prefix.Count);

        var fuzzy = _search.Search(null, "aple", "fuzzy");
        Assert.Contains(fuzzy, h => h.Headword == "apple");

        var fulltext = _search.Search(null, "computer", "fulltext");
        Assert.Single(fulltext);
        Assert.Equal("application", fulltext[0].Headword);
    }

    [Fact]
    public void Search_ignores_disabled_dictionaries()
    {
        var record = _dictionaries.Insert("Test Dict", "wordlist", "test.tsv", "hash2");
        _indexing.BuildIndex(record.Id, record.ContentHash, [new DictionaryEntry("cat", "A small mammal.")]);
        _dictionaries.SetEnabled(record.Id, false);

        var results = _search.Search(null, "cat", "exact");

        Assert.Empty(results);
    }

    [Fact]
    public void Fulltext_search_matches_across_stemmed_inflected_forms()
    {
        var record = _dictionaries.Insert("Test Dict", "wordlist", "test.tsv", "hash-stem", "en");
        _indexing.BuildIndex(record.Id, record.ContentHash,
            [new DictionaryEntry("run", "To move fast on foot.")],
            language: "en");
        _dictionaries.MarkIndexed(record.Id, DateTimeOffset.UtcNow.ToString("O"));

        var results = _search.Search(null, "running", "fulltext");

        Assert.Contains(results, h => h.Headword == "run");
    }

    [Fact]
    public void IsIndexed_is_false_before_building_and_true_after()
    {
        Assert.False(_indexing.IsIndexed("hash3"));

        _indexing.BuildIndex("dict-id", "hash3", [new DictionaryEntry("word", "definition")]);

        Assert.True(_indexing.IsIndexed("hash3"));
    }
}
