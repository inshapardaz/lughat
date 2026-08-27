using Lughat.Engine.Api.Formats;

namespace Lughat.Engine.Tests.Formats;

public class WordListProviderTests
{
    private static string FixturePath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "wordlist-dict", "fixture.tsv");

    [Fact]
    public void CanOpen_accepts_csv_tsv_and_txt()
    {
        var provider = new WordListProvider();

        Assert.True(provider.CanOpen("glossary.csv"));
        Assert.True(provider.CanOpen("glossary.tsv"));
        Assert.True(provider.CanOpen("glossary.txt"));
        Assert.False(provider.CanOpen("glossary.ifo"));
    }

    [Fact]
    public void ReadEntries_splits_term_and_definition_on_first_tab()
    {
        var provider = new WordListProvider();

        var entries = provider.ReadEntries(FixturePath).ToList();

        Assert.Equal(3, entries.Count);
        Assert.Equal("apple", entries[0].Headword);
        Assert.Contains("round fruit", entries[0].ArticleHtml);
    }

    [Fact]
    public void ReadEntries_throws_a_stable_error_code_for_a_malformed_row()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path + ".txt", "no-separator-on-this-line\n");
            var provider = new WordListProvider();

            var ex = Assert.Throws<DictionaryFormatException>(() => provider.ReadEntries(path + ".txt").ToList());
            Assert.Equal("dictionary.import.malformed_row", ex.ErrorCode);
        }
        finally
        {
            File.Delete(path);
            File.Delete(path + ".txt");
        }
    }
}
