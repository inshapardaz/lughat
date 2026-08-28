using System.IO.Compression;
using System.Text.RegularExpressions;

namespace Lughat.Engine.Api.Formats;

/// <summary>
/// ABBYY Lingvo DSL reader (spec §5). DSL is plain text: optional gzip compression
/// (.dsl.dz), a run of "#KEY value" header lines, then entries — one or more
/// column-0 headword lines (synonyms share one definition), followed by indented
/// definition lines using DSL's own bracket markup ([b]/[i]/[m1]/[trn]/etc.), which
/// this converts to the equivalent HTML. Encoding is usually UTF-16LE with a BOM;
/// <see cref="StreamReader"/>'s BOM detection handles that and plain UTF-8 alike.
/// </summary>
public sealed class DslProvider : IDictionaryProvider
{
    // Placeholders for escaped literal brackets (\[ \]) — swapped back to [ ] once tag
    // processing is done, so an escaped bracket never gets mistaken for real markup.
    private const string EscapedOpenPlaceholder = "";
    private const string EscapedClosePlaceholder = "";

    private static readonly Dictionary<string, string> SimpleTagMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["b"] = "b",
        ["i"] = "i",
        ["u"] = "u",
        ["sup"] = "sup",
        ["sub"] = "sub",
    };

    public string FormatId => "dsl";

    public bool CanOpen(string path) =>
        path.EndsWith(".dsl", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(".dsl.dz", StringComparison.OrdinalIgnoreCase);

    public IEnumerable<DictionaryEntry> ReadEntries(string path)
    {
        using var reader = OpenTextReader(path);
        var entries = new List<DictionaryEntry>();

        var line = reader.ReadLine();
        while (line is not null && line.StartsWith('#'))
        {
            line = reader.ReadLine();
        }

        var pendingHeadwords = new List<string>();
        var definitionLines = new List<string>();

        void FlushEntry()
        {
            if (pendingHeadwords.Count == 0)
            {
                return;
            }

            var article = ConvertMarkup(string.Join("<br>", definitionLines));
            foreach (var headword in pendingHeadwords)
            {
                entries.Add(new DictionaryEntry(headword.Trim(), article));
            }

            pendingHeadwords.Clear();
            definitionLines.Clear();
        }

        while (line is not null)
        {
            if (line.Length == 0)
            {
                line = reader.ReadLine();
                continue;
            }

            if (line[0] is ' ' or '\t')
            {
                definitionLines.Add(line.TrimStart());
            }
            else
            {
                // A new column-0 line starts a fresh entry once we've already seen its
                // definition — until then, consecutive column-0 lines are synonyms sharing
                // the one definition block that follows them.
                if (definitionLines.Count > 0)
                {
                    FlushEntry();
                }

                pendingHeadwords.Add(line);
            }

            line = reader.ReadLine();
        }

        FlushEntry();
        return entries;
    }

    private static StreamReader OpenTextReader(string path)
    {
        if (path.EndsWith(".dz", StringComparison.OrdinalIgnoreCase))
        {
            var fileStream = File.OpenRead(path);
            var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
            return new StreamReader(gzipStream, detectEncodingFromByteOrderMarks: true);
        }

        return new StreamReader(path, detectEncodingFromByteOrderMarks: true);
    }

    private static string ConvertMarkup(string dsl)
    {
        var text = dsl.Replace("\\[", EscapedOpenPlaceholder).Replace("\\]", EscapedClosePlaceholder);

        // Sound references and card-reference groups carry no textual content.
        text = Regex.Replace(text, @"\[s\].*?\[/s\]", string.Empty, RegexOptions.Singleline);
        text = Regex.Replace(text, @"\{\{.*?\}\}", string.Empty, RegexOptions.Singleline);

        foreach (var (dslTag, htmlTag) in SimpleTagMap)
        {
            text = Regex.Replace(text, $@"\[{dslTag}\]", $"<{htmlTag}>", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, $@"\[/{dslTag}\]", $"</{htmlTag}>", RegexOptions.IgnoreCase);
        }

        // Indentation markers [m1]..[/m] through [m9]..[/m].
        text = Regex.Replace(text, @"\[m(\d)\]", m => $"<div style=\"margin-inline-start:{int.Parse(m.Groups[1].Value) * 12}px\">");
        text = Regex.Replace(text, @"\[/m\]", "</div>");

        text = Regex.Replace(text, @"\[c(?:\s+\w+)?\]", "<span class=\"dsl-c\">");
        text = Regex.Replace(text, @"\[/c\]", "</span>");

        // Translation/part-of-speech/cross-reference/transcription/example — rendered as
        // plain emphasis; DSL-specific behaviour (cross-reference navigation, colour
        // swatches by name) isn't otherwise acted on.
        text = Regex.Replace(text, @"\[(trn|p|ref|t|ex)\]", "<i>");
        text = Regex.Replace(text, @"\[/(trn|p|ref|t|ex)\]", "</i>");

        // Anything else: strip the brackets but keep going — DSL has many rarer tags this
        // doesn't special-case, and dropping just the markup beats losing the whole article.
        text = Regex.Replace(text, @"\[/?\w+(?:\s+[^\]]*)?\]", string.Empty);

        return text.Replace(EscapedOpenPlaceholder, "[").Replace(EscapedClosePlaceholder, "]");
    }
}
