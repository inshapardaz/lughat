using Lughat.Engine.Api.Formats;

namespace Lughat.Engine.Tests.Formats;

public class MdxProviderTests
{
    private static string FixturePath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "mdx-dict", "fixture.mdx");

    [Fact]
    public void CanOpen_accepts_mdx_files_only()
    {
        var provider = new MdxProvider();

        Assert.True(provider.CanOpen("dict.mdx"));
        Assert.False(provider.CanOpen("dict.mdd"));
        Assert.False(provider.CanOpen("dict.ifo"));
    }

    [Fact]
    public void ReadEntries_decompresses_key_and_record_blocks_correctly()
    {
        var provider = new MdxProvider();

        var entries = provider.ReadEntries(FixturePath).ToDictionary(e => e.Headword, e => e.ArticleHtml);

        Assert.Equal(3, entries.Count);
        Assert.Equal("<b>apple</b> — a round fruit with red or green skin.", entries["apple"]);
        Assert.Equal("<b>book</b> — a set of printed pages bound together.", entries["book"]);
        Assert.Equal("<b>cat</b> — a small domesticated carnivorous mammal.", entries["cat"]);
    }
}
