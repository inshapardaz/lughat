using System.Xml;
using System.Xml.Linq;

namespace Lughat.Engine.Api.Formats;

/// <summary>
/// XDXF (XML Dictionary eXchange Format) reader (spec §5). Supports the common "visual"
/// XDXF layout — a run of &lt;ar&gt; (article) elements, each with one or more &lt;k&gt;
/// (headword) children sharing one or more &lt;def&gt; children, whose inner markup
/// (&lt;b&gt;/&lt;i&gt;/etc.) already reads as HTML. XDXF's less common "logical" format
/// (semantic tags like &lt;gr&gt;/&lt;deftext&gt; instead of visual markup) isn't specially
/// handled — its raw tags pass through as-is, same graceful-degradation approach as the
/// DSL provider's unmapped tags.
/// </summary>
public sealed class XdxfProvider : IDictionaryProvider
{
    public string FormatId => "xdxf";

    public bool CanOpen(string path) => path.EndsWith(".xdxf", StringComparison.OrdinalIgnoreCase);

    public IEnumerable<DictionaryEntry> ReadEntries(string path)
    {
        XDocument document;
        try
        {
            document = XDocument.Load(path, LoadOptions.PreserveWhitespace);
        }
        catch (XmlException ex)
        {
            throw new DictionaryFormatException("dictionary.import.corrupt_header", $"{path} isn't valid XML: {ex.Message}");
        }

        var root = document.Root
            ?? throw new DictionaryFormatException("dictionary.import.corrupt_header", $"{path} has no root element.");

        var entries = new List<DictionaryEntry>();
        foreach (var article in root.Elements("ar"))
        {
            var headwords = article.Elements("k").Select(k => k.Value.Trim()).Where(k => k.Length > 0).ToList();
            var definitions = article.Elements("def").ToList();
            if (headwords.Count == 0 || definitions.Count == 0)
            {
                continue;
            }

            var articleHtml = string.Join("<br>", definitions.Select(GetInnerMarkup));
            foreach (var headword in headwords)
            {
                entries.Add(new DictionaryEntry(headword, articleHtml));
            }
        }

        return entries;
    }

    private static string GetInnerMarkup(XElement element) =>
        string.Concat(element.Nodes().Select(node => node.ToString(SaveOptions.DisableFormatting)));
}
