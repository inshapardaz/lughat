using Lughat.Engine.Api.Formats;

namespace Lughat.Engine.Tests.Formats;

public class DictionaryProviderRegistryTests
{
    [Fact]
    public void FindProviderForPath_picks_the_provider_that_claims_the_extension()
    {
        var registry = new DictionaryProviderRegistry()
            .Register(new StarDictProvider())
            .Register(new WordListProvider())
            .Register(new MdxProvider());

        Assert.IsType<StarDictProvider>(registry.FindProviderForPath("foo.ifo"));
        Assert.IsType<WordListProvider>(registry.FindProviderForPath("foo.csv"));
        Assert.IsType<MdxProvider>(registry.FindProviderForPath("foo.mdx"));
        Assert.Null(registry.FindProviderForPath("foo.unknown"));
    }

    [Fact]
    public void FindProviderById_looks_up_by_the_stable_format_id()
    {
        var registry = new DictionaryProviderRegistry().Register(new StarDictProvider());

        Assert.IsType<StarDictProvider>(registry.FindProviderById("stardict"));
        Assert.Null(registry.FindProviderById("nonexistent"));
    }
}
