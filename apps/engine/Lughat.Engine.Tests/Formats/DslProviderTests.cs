using Lughat.Engine.Api.Formats;

namespace Lughat.Engine.Tests.Formats;

public class DslProviderTests
{
    private static string FixturePath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "dsl-dict", "fixture.dsl");

    [Fact]
    public void CanOpen_accepts_dsl_and_dsl_dz()
    {
        var provider = new DslProvider();

        Assert.True(provider.CanOpen("dict.dsl"));
        Assert.True(provider.CanOpen("dict.dsl.dz"));
        Assert.False(provider.CanOpen("dict.ifo"));
    }

    [Fact]
    public void ReadEntries_skips_headers_and_converts_markup_to_html()
    {
        var provider = new DslProvider();

        var entries = provider.ReadEntries(FixturePath).ToDictionary(e => e.Headword, e => e.ArticleHtml);

        Assert.Equal(4, entries.Count); // apple, book, couch, sofa
        Assert.Equal(
            "<b>apple</b> — <div style=\"margin-inline-start:12px\">a round fruit that grows on trees</div>",
            entries["apple"]);
        Assert.Equal("a set of printed pages bound together", entries["book"]);
    }

    [Fact]
    public void ReadEntries_shares_one_definition_across_consecutive_headwords()
    {
        var provider = new DslProvider();

        var entries = provider.ReadEntries(FixturePath).ToDictionary(e => e.Headword, e => e.ArticleHtml);

        Assert.Equal(entries["couch"], entries["sofa"]);
        Assert.Contains("also called a couch or sofa", entries["couch"]);
    }
}
