using Lughat.Engine.Api.Formats;

namespace Lughat.Engine.Tests.Formats;

public class XdxfProviderTests
{
    private static string FixturePath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "xdxf-dict", "fixture.xdxf");

    [Fact]
    public void CanOpen_accepts_xdxf_files_only()
    {
        var provider = new XdxfProvider();

        Assert.True(provider.CanOpen("dict.xdxf"));
        Assert.False(provider.CanOpen("dict.dsl"));
    }

    [Fact]
    public void ReadEntries_preserves_inner_markup_and_shares_definitions_across_keys()
    {
        var provider = new XdxfProvider();

        var entries = provider.ReadEntries(FixturePath).ToDictionary(e => e.Headword, e => e.ArticleHtml);

        Assert.Equal(4, entries.Count); // apple, book, couch, sofa
        Assert.Equal("<b>apple</b> — a round fruit that grows on trees.", entries["apple"]);
        Assert.Equal("a set of printed pages bound together.", entries["book"]);
        Assert.Equal(entries["couch"], entries["sofa"]);
    }

    [Fact]
    public void ReadEntries_throws_a_stable_error_code_for_malformed_xml()
    {
        var path = Path.GetTempFileName() + ".xdxf";
        try
        {
            File.WriteAllText(path, "<xdxf><ar><k>oops</k>");
            var provider = new XdxfProvider();

            var ex = Assert.Throws<DictionaryFormatException>(() => provider.ReadEntries(path).ToList());
            Assert.Equal("dictionary.import.corrupt_header", ex.ErrorCode);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
