using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;

namespace Lughat.Engine.Api.Search;

/// <summary>
/// Runs each token through an <see cref="IStemmer"/> after standard tokenizing/lowercasing.
/// Used only for the stemmed shadow fields ("headwordStemmed"/"articleStemmed") — the plain
/// "headword"/"article" fields keep exact tokens so unstemmed search behavior is unaffected.
/// </summary>
public sealed class StemmingAnalyzer(LuceneVersion version, IStemmer stemmer) : Analyzer
{
    protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
    {
        var tokenizer = new StandardTokenizer(version, reader);
        TokenStream stream = new StandardFilter(version, tokenizer);
        stream = new LowerCaseFilter(version, stream);
        stream = new StemmingTokenFilter(stream, stemmer);
        return new TokenStreamComponents(tokenizer, stream);
    }
}

internal sealed class StemmingTokenFilter(TokenStream input, IStemmer stemmer) : TokenFilter(input)
{
    private readonly ICharTermAttribute _termAttribute = input.AddAttribute<ICharTermAttribute>();

    public override bool IncrementToken()
    {
        if (!m_input.IncrementToken())
        {
            return false;
        }

        var stemmed = stemmer.Stem(_termAttribute.ToString());
        _termAttribute.SetEmpty().Append(stemmed);
        return true;
    }
}
