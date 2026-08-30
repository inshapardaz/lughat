using Lughat.Engine.Api.Formats;

namespace Lughat.Engine.Tests.Formats;

public class KaikkiProviderTests
{
    private static string FixturePath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "kaikki-dict", "fixture.jsonl");

    [Fact]
    public void CanOpen_accepts_jsonl_files_only()
    {
        var provider = new KaikkiProvider();

        Assert.True(provider.CanOpen("dump.jsonl"));
        Assert.False(provider.CanOpen("dump.json"));
    }

    [Fact]
    public void ReadEntries_merges_multiple_part_of_speech_lines_for_the_same_word()
    {
        var provider = new KaikkiProvider();

        var entries = provider.ReadEntries(FixturePath).ToDictionary(e => e.Headword, e => e.ArticleHtml);

        Assert.Contains("apple", entries.Keys);
        Assert.Contains("To move fast on foot.", entries["run"]);
        Assert.Contains("An act of running.", entries["run"]);
        Assert.Contains("<em>verb</em>", entries["run"]);
        Assert.Contains("<em>noun</em>", entries["run"]);
    }

    [Fact]
    public void ReadEntries_skips_lines_with_no_word_or_no_glosses()
    {
        var provider = new KaikkiProvider();

        var entries = provider.ReadEntries(FixturePath).ToDictionary(e => e.Headword, e => e.ArticleHtml);

        Assert.DoesNotContain(string.Empty, entries.Keys);
        Assert.DoesNotContain("empty-senses", entries.Keys);
    }

    [Fact]
    public void ReadEntries_throws_a_stable_error_code_for_malformed_json()
    {
        var path = Path.GetTempFileName() + ".jsonl";
        try
        {
            File.WriteAllText(path, "{not valid json");
            var provider = new KaikkiProvider();

            var ex = Assert.Throws<DictionaryFormatException>(() => provider.ReadEntries(path).ToList());
            Assert.Equal("dictionary.import.corrupt_header", ex.ErrorCode);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
