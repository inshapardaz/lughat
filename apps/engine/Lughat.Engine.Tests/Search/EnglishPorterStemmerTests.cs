using Lughat.Engine.Api.Search;

namespace Lughat.Engine.Tests.Search;

public class EnglishPorterStemmerTests
{
    private readonly EnglishPorterStemmer _stemmer = new();

    [Theory]
    [InlineData("running", "run")]
    [InlineData("runs", "run")]
    [InlineData("run", "run")]
    [InlineData("caresses", "caress")]
    [InlineData("ponies", "poni")]
    [InlineData("agreed", "agre")]
    [InlineData("plastered", "plaster")]
    [InlineData("motoring", "motor")]
    [InlineData("sensational", "sensat")]
    [InlineData("relational", "relat")]
    [InlineData("conditional", "condit")]
    public void Stem_reduces_inflected_forms_to_their_root(string input, string expected)
    {
        Assert.Equal(expected, _stemmer.Stem(input));
    }

    [Fact]
    public void LanguageCode_is_en()
    {
        Assert.Equal("en", _stemmer.LanguageCode);
    }
}

public class StemmerRegistryTests
{
    [Fact]
    public void Get_returns_the_registered_stemmer_for_a_known_language()
    {
        var stemmer = StemmerRegistry.Default.Get("en");

        Assert.Equal("en", stemmer.LanguageCode);
        Assert.Equal("run", stemmer.Stem("running"));
    }

    [Fact]
    public void Get_falls_back_to_a_no_op_stemmer_for_an_unregistered_language()
    {
        var stemmer = StemmerRegistry.Default.Get("xx");

        Assert.Equal("running", stemmer.Stem("running"));
    }

    [Fact]
    public void Get_falls_back_to_a_no_op_stemmer_when_language_is_null()
    {
        var stemmer = StemmerRegistry.Default.Get(null);

        Assert.Equal("running", stemmer.Stem("running"));
    }
}
