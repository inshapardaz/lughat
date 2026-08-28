using System.Text.Encodings.Web;

namespace Lughat.Engine.Api.Formats;

/// <summary>
/// Plain word-list glossaries: one entry per line, term and definition separated by the
/// first tab (.tsv), first comma (.csv), or first tab falling back to first comma (.txt).
/// Spec §5 — the trivial-but-high-value MVP format for user glossaries and quick imports.
/// </summary>
public sealed class WordListProvider : IDictionaryProvider
{
    public string FormatId => "wordlist";

    public bool CanOpen(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext is ".csv" or ".tsv" or ".txt";
    }

    public IEnumerable<DictionaryEntry> ReadEntries(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        var lineNumber = 0;

        foreach (var line in File.ReadLines(path))
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var separator = ext == ".csv" ? ',' : '\t';
            var separatorIndex = line.IndexOf(separator);
            if (separatorIndex < 0)
            {
                // .txt files may use either delimiter — fall back to comma if no tab was found.
                separatorIndex = line.IndexOf(',');
            }

            if (separatorIndex < 0)
            {
                throw new DictionaryFormatException(
                    "dictionary.import.malformed_row",
                    $"{path}:{lineNumber} has no tab or comma separating term from definition.");
            }

            var term = line[..separatorIndex].Trim();
            var definition = line[(separatorIndex + 1)..].Trim();
            if (term.Length == 0)
            {
                continue;
            }

            yield return new DictionaryEntry(term, HtmlEncoder.Default.Encode(definition));
        }
    }
}
