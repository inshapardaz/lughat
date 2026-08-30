using Lughat.Engine.Api.Formats;

namespace Lughat.Engine.Tests.Formats;

public class WordNetProviderTests
{
    private static string FixturePath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "wordnet-dict", "index.sense");

    [Fact]
    public void CanOpen_accepts_only_the_index_sense_marker_file()
    {
        var provider = new WordNetProvider();

        Assert.True(provider.CanOpen(FixturePath));
        Assert.False(provider.CanOpen("index.noun"));
        Assert.False(provider.CanOpen("data.noun"));
    }

    [Fact]
    public void ReadEntries_returns_one_entry_per_lemma_across_a_synset()
    {
        var provider = new WordNetProvider();

        var entries = provider.ReadEntries(FixturePath).ToDictionary(e => e.Headword, e => e.ArticleHtml);

        Assert.Equal(5, entries.Count); // animal, dog, canine, cat, feline
        Assert.Contains("a domesticated carnivorous mammal.", entries["dog"]);
        Assert.Contains("a small domesticated carnivorous mammal.", entries["cat"]);
    }

    [Fact]
    public void ReadEntries_surfaces_synonyms_from_the_shared_synset()
    {
        var provider = new WordNetProvider();

        var entries = provider.ReadEntries(FixturePath).ToDictionary(e => e.Headword, e => e.ArticleHtml);

        Assert.Contains("Synonyms:</em> <a href=\"#\">canine</a>", entries["dog"]);
        Assert.Contains("Synonyms:</em> <a href=\"#\">dog</a>", entries["canine"]);
    }

    [Fact]
    public void ReadEntries_surfaces_the_hypernym_as_a_browsable_cross_reference()
    {
        var provider = new WordNetProvider();

        var entries = provider.ReadEntries(FixturePath).ToDictionary(e => e.Headword, e => e.ArticleHtml);

        Assert.Contains("Broader term:</em> <a href=\"#\">animal</a>", entries["dog"]);
        Assert.Contains("Broader term:</em> <a href=\"#\">animal</a>", entries["cat"]);
        Assert.DoesNotContain("Broader term", entries["animal"]);
    }
}
