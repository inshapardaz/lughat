using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Lughat.Engine.Api.Formats;

/// <summary>
/// Reads a Kaikki.org (Wiktextract) JSONL dump — one JSON object per line, each describing
/// one word/part-of-speech entry with a "word", optional "pos", and "senses[].glosses[]"
/// (spec §5/§6/§15, issue #58). Multiple lines for the same word (different parts of speech,
/// or multiple etymologies) are merged into a single article, same "aggregate by shared
/// headword" approach DslProvider/XdxfProvider already use. Only the fields this app actually
/// surfaces (word, pos, glosses) are read — a real Kaikki dump carries far more (etymology,
/// pronunciation audio, translations, forms) that a future pass can pull in without changing
/// this provider's shape.
/// </summary>
public sealed class KaikkiProvider : IDictionaryProvider
{
    public string FormatId => "kaikki";

    public bool CanOpen(string path) => path.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase);

    public IEnumerable<DictionaryEntry> ReadEntries(string path)
    {
        var blocksByWord = new Dictionary<string, List<string>>();
        var lineNumber = 0;

        foreach (var line in File.ReadLines(path))
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            using var document = ParseLine(line, path, lineNumber);
            var root = document.RootElement;

            if (!root.TryGetProperty("word", out var wordProp) || wordProp.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var word = wordProp.GetString();
            if (string.IsNullOrWhiteSpace(word))
            {
                continue;
            }

            var pos = root.TryGetProperty("pos", out var posProp) && posProp.ValueKind == JsonValueKind.String
                ? posProp.GetString()
                : null;

            var glosses = ReadGlosses(root);
            if (glosses.Count == 0)
            {
                continue;
            }

            if (!blocksByWord.TryGetValue(word, out var blocks))
            {
                blocks = [];
                blocksByWord[word] = blocks;
            }

            blocks.Add(BuildBlockHtml(pos, glosses));
        }

        foreach (var (word, blocks) in blocksByWord)
        {
            yield return new DictionaryEntry(word, string.Join(string.Empty, blocks));
        }
    }

    private static JsonDocument ParseLine(string line, string path, int lineNumber)
    {
        try
        {
            return JsonDocument.Parse(line);
        }
        catch (JsonException ex)
        {
            throw new DictionaryFormatException(
                "dictionary.import.corrupt_header",
                $"Malformed JSON on line {lineNumber} of {path}: {ex.Message}");
        }
    }

    private static List<string> ReadGlosses(JsonElement root)
    {
        var glosses = new List<string>();
        if (!root.TryGetProperty("senses", out var senses) || senses.ValueKind != JsonValueKind.Array)
        {
            return glosses;
        }

        foreach (var sense in senses.EnumerateArray())
        {
            if (!sense.TryGetProperty("glosses", out var glossArray) || glossArray.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var gloss in glossArray.EnumerateArray())
            {
                if (gloss.ValueKind == JsonValueKind.String && gloss.GetString() is { Length: > 0 } text)
                {
                    glosses.Add(text);
                }
            }
        }

        return glosses;
    }

    private static string BuildBlockHtml(string? pos, List<string> glosses)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(pos))
        {
            sb.Append("<p><em>").Append(HtmlEncoder.Default.Encode(pos)).Append("</em></p>");
        }

        sb.Append("<ol>");
        foreach (var gloss in glosses)
        {
            sb.Append("<li>").Append(HtmlEncoder.Default.Encode(gloss)).Append("</li>");
        }

        sb.Append("</ol>");
        return sb.ToString();
    }
}
