using System.Text;

namespace Lughat.Engine.Api.Formats;

/// <summary>
/// Spike-quality StarDict reader (issue: "Spike: StarDict provider").
/// Only supports uncompressed .idx/.dict files with sametypesequence=m (plain text).
/// The production provider (Phase 1) replaces this with full StarDict support,
/// including .dict.dz and mixed-type entries — see spec §5 / §15.
/// </summary>
public static class StarDictReader
{
    public static IReadOnlyList<StarDictEntry> Read(string ifoPath)
    {
        var dir = Path.GetDirectoryName(ifoPath)!;
        var baseName = Path.GetFileNameWithoutExtension(ifoPath);
        var idxPath = Path.Combine(dir, baseName + ".idx");
        var dictPath = Path.Combine(dir, baseName + ".dict");

        var ifo = ParseIfo(ifoPath);
        if (!ifo.TryGetValue("sametypesequence", out var sequence) || sequence != "m")
        {
            throw new NotSupportedException(
                "The spike StarDict reader only supports sametypesequence=m (plain text) dictionaries.");
        }

        var dictBytes = File.ReadAllBytes(dictPath);
        var idxBytes = File.ReadAllBytes(idxPath);

        var entries = new List<StarDictEntry>();
        var pos = 0;
        while (pos < idxBytes.Length)
        {
            var wordEnd = Array.IndexOf(idxBytes, (byte)0, pos);
            var word = Encoding.UTF8.GetString(idxBytes, pos, wordEnd - pos);
            pos = wordEnd + 1;

            var offset = ReadUInt32BigEndian(idxBytes, pos);
            pos += 4;
            var size = ReadUInt32BigEndian(idxBytes, pos);
            pos += 4;

            var article = Encoding.UTF8.GetString(dictBytes, (int)offset, (int)size);
            entries.Add(new StarDictEntry(word, article));
        }

        return entries;
    }

    private static uint ReadUInt32BigEndian(byte[] buffer, int offset) =>
        (uint)((buffer[offset] << 24) | (buffer[offset + 1] << 16) | (buffer[offset + 2] << 8) | buffer[offset + 3]);

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

public sealed record StarDictEntry(string Headword, string Article);
