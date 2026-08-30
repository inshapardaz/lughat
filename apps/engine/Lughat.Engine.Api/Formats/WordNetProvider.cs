using System.Text;
using System.Text.Encodings.Web;

namespace Lughat.Engine.Api.Formats;

/// <summary>
/// WordNet (WNDB — the on-disk format the Princeton WordNet package ships) reader (spec §5).
/// Unlike every other provider, a WordNet lookup isn't "one headword, one article" — a lemma
/// belongs to one or more synsets (sets of synonyms sharing a sense), and synsets link to
/// other synsets via typed pointers. This provider flattens that relational model down to
/// one <see cref="DictionaryEntry"/> per lemma, but keeps the synonym/hypernym relationships
/// browsable as in-article cross-reference links (rendered the same way DSL/XDXF articles
/// already do — a plain non-http &lt;a&gt;, whose text the reader's click handler treats as
/// the next lookup term, see ArticleView.tsx).
///
/// Import points at the dict directory's "index.sense" file (same "point at one marker file,
/// resolve siblings by convention" pattern StarDictProvider uses for .ifo); data.{noun,verb,
/// adj,adv} and index.{noun,verb,adj,adv} are read from the same directory. Only the
/// hypernym pointer ("@" / "@i") is surfaced — WordNet defines dozens of pointer types
/// (meronym, antonym, ...), but hypernym is the one relationship every synset in the
/// distribution reliably carries, and is enough to make browsing "broader term" meaningful
/// without guessing at how to label every other pointer symbol.
/// </summary>
public sealed class WordNetProvider : IDictionaryProvider
{
    private static readonly string[] PartsOfSpeech = ["noun", "verb", "adj", "adv"];

    public string FormatId => "wordnet";

    public bool CanOpen(string path) =>
        string.Equals(Path.GetFileName(path), "index.sense", StringComparison.OrdinalIgnoreCase);

    public IEnumerable<DictionaryEntry> ReadEntries(string path)
    {
        var dir = Path.GetDirectoryName(path)!;

        var synsets = new Dictionary<(char Pos, string Offset), Synset>();
        foreach (var pos in PartsOfSpeech)
        {
            var dataPath = Path.Combine(dir, $"data.{pos}");
            if (!File.Exists(dataPath))
            {
                continue;
            }

            foreach (var line in File.ReadLines(dataPath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith(' '))
                {
                    continue; // license-header lines in real WordNet data files start with a space
                }

                var synset = ParseDataLine(line, dataPath);
                synsets[(synset.Pos, synset.Offset)] = synset;
            }
        }

        if (synsets.Count == 0)
        {
            throw new DictionaryFormatException(
                "dictionary.import.missing_file",
                $"No WordNet data.* files found next to {path}.");
        }

        var lemmaToSynsets = new Dictionary<string, List<(char Pos, string Offset)>>(StringComparer.OrdinalIgnoreCase);
        foreach (var pos in PartsOfSpeech)
        {
            var indexPath = Path.Combine(dir, $"index.{pos}");
            if (!File.Exists(indexPath))
            {
                continue;
            }

            foreach (var line in File.ReadLines(indexPath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith(' '))
                {
                    continue;
                }

                ParseIndexLine(line, lemmaToSynsets);
            }
        }

        foreach (var (lemma, refs) in lemmaToSynsets)
        {
            var articleHtml = BuildArticleHtml(lemma, refs, synsets);
            if (articleHtml is not null)
            {
                yield return new DictionaryEntry(lemma.Replace('_', ' '), articleHtml);
            }
        }
    }

    private static Synset ParseDataLine(string line, string sourcePath)
    {
        var barIndex = line.IndexOf('|');
        var gloss = barIndex >= 0 ? line[(barIndex + 1)..].Trim() : string.Empty;
        var head = barIndex >= 0 ? line[..barIndex] : line;
        var tokens = head.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 4)
        {
            throw new DictionaryFormatException("dictionary.import.corrupt_index", $"Malformed WordNet data line in {sourcePath}.");
        }

        var offset = tokens[0];
        var pos = tokens[2][0];
        var wordCount = Convert.ToInt32(tokens[3], 16);

        var index = 4;
        var words = new List<string>();
        for (var i = 0; i < wordCount; i++)
        {
            words.Add(tokens[index]);
            index += 2; // word, lex_id
        }

        var pointerCount = int.Parse(tokens[index]);
        index += 1;
        var hypernyms = new List<(char Pos, string Offset)>();
        for (var i = 0; i < pointerCount; i++)
        {
            var symbol = tokens[index];
            var targetOffset = tokens[index + 1];
            var targetPos = tokens[index + 2][0];
            index += 4; // pointer_symbol, synset_offset, pos, source/target

            if (symbol is "@" or "@i")
            {
                hypernyms.Add((targetPos, targetOffset));
            }
        }

        return new Synset(pos, offset, words, hypernyms, gloss);
    }

    private static void ParseIndexLine(string line, Dictionary<string, List<(char Pos, string Offset)>> lemmaToSynsets)
    {
        var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 4)
        {
            return;
        }

        var lemma = tokens[0];
        var pos = tokens[1][0];
        var pointerSymbolCount = int.Parse(tokens[3]);
        var index = 4 + pointerSymbolCount + 2; // skip pointer symbols, then sense_cnt, tagsense_cnt

        var offsets = new List<(char Pos, string Offset)>();
        for (; index < tokens.Length; index++)
        {
            offsets.Add((pos, tokens[index]));
        }

        lemmaToSynsets[lemma] = offsets;
    }

    private static string? BuildArticleHtml(
        string lemma,
        List<(char Pos, string Offset)> refs,
        Dictionary<(char Pos, string Offset), Synset> synsets)
    {
        var blocks = new List<string>();
        foreach (var reference in refs)
        {
            if (!synsets.TryGetValue(reference, out var synset))
            {
                continue;
            }

            var synonyms = synset.Words
                .Where(w => !string.Equals(w, lemma, StringComparison.OrdinalIgnoreCase))
                .Select(w => $"<a href=\"#\">{HtmlEncoder.Default.Encode(w.Replace('_', ' '))}</a>")
                .ToList();

            var hypernyms = synset.Hypernyms
                .Select(h => synsets.TryGetValue(h, out var target) ? target.Words.FirstOrDefault() : null)
                .Where(w => w is not null)
                .Select(w => $"<a href=\"#\">{HtmlEncoder.Default.Encode(w!.Replace('_', ' '))}</a>")
                .ToList();

            var sb = new StringBuilder();
            sb.Append("<p>").Append(HtmlEncoder.Default.Encode(synset.Gloss)).Append("</p>");
            if (synonyms.Count > 0)
            {
                sb.Append("<p><em>Synonyms:</em> ").Append(string.Join(", ", synonyms)).Append("</p>");
            }

            if (hypernyms.Count > 0)
            {
                sb.Append("<p><em>Broader term:</em> ").Append(string.Join(", ", hypernyms)).Append("</p>");
            }

            blocks.Add(sb.ToString());
        }

        return blocks.Count == 0 ? null : string.Join("<hr>", blocks);
    }

    private sealed record Synset(char Pos, string Offset, List<string> Words, List<(char Pos, string Offset)> Hypernyms, string Gloss);
}
