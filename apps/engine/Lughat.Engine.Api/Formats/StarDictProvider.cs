using System.IO.Compression;
using System.Text;
using System.Text.Encodings.Web;

namespace Lughat.Engine.Api.Formats;

/// <summary>
/// StarDict reader (spec §5). Supports the common on-disk shapes: plain or gzip-compressed
/// .idx (.idx/.idx.gz), plain or dictzip-compressed .dict (.dict/.dict.dz — dictzip is a
/// valid gzip stream when read sequentially, which is all this needs), and both the
/// single-type ("sametypesequence") and mixed-type (per-entry type markers) article layouts.
///
/// Binary resource types (sound/image entries embedded directly in the .dict data, as
/// opposed to files in a dictionary's res/ folder) are skipped rather than extracted — a
/// known gap, not a silent-corruption risk: text types are still parsed correctly around them.
/// </summary>
public sealed class StarDictProvider : IDictionaryProvider
{
    private const string TextTypes = "mlgtxykwh";

    public string FormatId => "stardict";

    public bool CanOpen(string path) => path.EndsWith(".ifo", StringComparison.OrdinalIgnoreCase);

    public IEnumerable<DictionaryEntry> ReadEntries(string path)
    {
        var dir = Path.GetDirectoryName(path)!;
        var baseName = Path.GetFileNameWithoutExtension(path);

        var ifo = ParseIfo(path);
        var sameType = ifo.GetValueOrDefault("sametypesequence");

        var idxBytes = ReadMaybeGzipped(Path.Combine(dir, baseName + ".idx"), Path.Combine(dir, baseName + ".idx.gz"));
        var dictBytes = ReadMaybeGzipped(Path.Combine(dir, baseName + ".dict"), Path.Combine(dir, baseName + ".dict.dz"));

        foreach (var (word, offset, size) in ReadIndexEntries(idxBytes, path))
        {
            if (offset < 0 || size < 0 || offset + size > dictBytes.Length)
            {
                throw new DictionaryFormatException(
                    "dictionary.import.corrupt_index",
                    $"StarDict index entry for '{word}' points outside the .dict file in {path}.");
            }

            var article = string.IsNullOrEmpty(sameType)
                ? ExtractMixedTypeArticle(dictBytes, (int)offset, (int)size)
                : ExtractSameTypeArticle(dictBytes, (int)offset, (int)size, sameType[0]);

            yield return new DictionaryEntry(word, article);
        }
    }

    private static IEnumerable<(string Word, long Offset, long Size)> ReadIndexEntries(byte[] idxBytes, string sourcePath)
    {
        var pos = 0;
        while (pos < idxBytes.Length)
        {
            var wordEnd = Array.IndexOf(idxBytes, (byte)0, pos);
            if (wordEnd < 0 || wordEnd + 9 > idxBytes.Length)
            {
                throw new DictionaryFormatException(
                    "dictionary.import.corrupt_index",
                    $"Truncated .idx entry while reading {sourcePath}.");
            }

            var word = Encoding.UTF8.GetString(idxBytes, pos, wordEnd - pos);
            pos = wordEnd + 1;

            var offset = ReadUInt32BigEndian(idxBytes, pos);
            pos += 4;
            var size = ReadUInt32BigEndian(idxBytes, pos);
            pos += 4;

            yield return (word, offset, size);
        }
    }

    private static string ExtractSameTypeArticle(byte[] dictBytes, int offset, int size, char type)
    {
        var raw = Encoding.UTF8.GetString(dictBytes, offset, size);
        return TextTypes.Contains(char.ToLowerInvariant(type)) && type != 'h' && type != 'g' && type != 'x'
            ? HtmlEncoder.Default.Encode(raw)
            : raw;
    }

    private static string ExtractMixedTypeArticle(byte[] dictBytes, int offset, int size)
    {
        var end = offset + size;
        var pos = offset;
        var sb = new StringBuilder();

        while (pos < end)
        {
            var type = (char)dictBytes[pos];
            pos += 1;

            if (char.IsUpper(type))
            {
                // Binary resource segment (sound/image/etc.) — not extracted in this pass.
                // Per the StarDict layout it's either size-prefixed or runs to the entry's
                // end; without a size prefix there is nothing left to parse after it, so
                // stop rather than misread the remaining bytes as another segment.
                if (pos + 4 > end)
                {
                    break;
                }

                var resourceSize = (int)ReadUInt32LittleEndian(dictBytes, pos);
                pos += 4 + resourceSize;
                continue;
            }

            var segmentEnd = Array.IndexOf(dictBytes, (byte)0, pos, end - pos);
            if (segmentEnd < 0)
            {
                segmentEnd = end;
            }

            var text = Encoding.UTF8.GetString(dictBytes, pos, segmentEnd - pos);
            sb.Append(type is 'h' or 'g' or 'x' ? text : HtmlEncoder.Default.Encode(text));
            pos = segmentEnd + 1;
        }

        return sb.ToString();
    }

    private static byte[] ReadMaybeGzipped(string plainPath, string gzipPath)
    {
        if (File.Exists(plainPath))
        {
            return File.ReadAllBytes(plainPath);
        }

        if (File.Exists(gzipPath))
        {
            using var fileStream = File.OpenRead(gzipPath);
            using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
            using var memoryStream = new MemoryStream();
            gzipStream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }

        throw new DictionaryFormatException(
            "dictionary.import.missing_file",
            $"Neither {plainPath} nor {gzipPath} exists.");
    }

    private static uint ReadUInt32BigEndian(byte[] buffer, int offset) =>
        (uint)((buffer[offset] << 24) | (buffer[offset + 1] << 16) | (buffer[offset + 2] << 8) | buffer[offset + 3]);

    private static uint ReadUInt32LittleEndian(byte[] buffer, int offset) =>
        (uint)(buffer[offset] | (buffer[offset + 1] << 8) | (buffer[offset + 2] << 16) | (buffer[offset + 3] << 24));

    private static Dictionary<string, string> ParseIfo(string path)
    {
        var result = new Dictionary<string, string>();
        // First line is the literal header "StarDict's dict ifo file" — not a key=value pair.
        foreach (var line in File.ReadAllLines(path).Skip(1))
        {
            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            result[line[..separator]] = line[(separator + 1)..];
        }

        return result;
    }
}
