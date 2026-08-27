using Lughat.Engine.Api.Formats;

namespace Lughat.Engine.Tests.Formats;

public class StarDictProviderTests
{
    private static string FixturePath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "spike-dict", "spike-dict.ifo");

    [Fact]
    public void CanOpen_accepts_ifo_files_only()
    {
        var provider = new StarDictProvider();

        Assert.True(provider.CanOpen("dict.ifo"));
        Assert.False(provider.CanOpen("dict.idx"));
        Assert.False(provider.CanOpen("dict.mdx"));
    }

    [Fact]
    public void ReadEntries_returns_every_headword_with_its_article()
    {
        var provider = new StarDictProvider();

        var entries = provider.ReadEntries(FixturePath).ToDictionary(e => e.Headword, e => e.ArticleHtml);

        Assert.Equal(3, entries.Count);
        Assert.Equal("A round fruit with red or green skin and crisp flesh.", entries["apple"]);
        Assert.Equal("A set of printed pages bound together for reading.", entries["book"]);
        Assert.Equal("A small domesticated carnivorous mammal.", entries["cat"]);
    }
}
