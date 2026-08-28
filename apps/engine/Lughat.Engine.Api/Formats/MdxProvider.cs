using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace Lughat.Engine.Api.Formats;

/// <summary>
/// MDict (.mdx) reader (spec §5 / §15). MDX has no official public specification — this
/// follows the layout documented by the community (writemdict/pyglossary/mdict-utils):
/// a UTF-16LE XML header, then a key-block-info section, key blocks, record-block-info
/// section, and record blocks, each compressed block prefixed with a 4-byte compression
/// type + 4-byte checksum (the checksum is read but not verified here).
///
/// Supports the common case: MDX 2.0's 8-byte block-size fields, and zlib or uncompressed
/// blocks. Encrypted files and LZO-compressed blocks (compression type 1) are explicitly
/// unsupported and raise a clear error code rather than misreading — see the "Encrypted/
/// unsupported files surface a clear error code" acceptance criteria on the MDX provider
/// issue. MDX &lt; 2.0's 4-byte fields are likewise unsupported for now.
/// </summary>
public sealed class MdxProvider : IDictionaryProvider
{
    public string FormatId => "mdx";

    public bool CanOpen(string path) => path.EndsWith(".mdx", StringComparison.OrdinalIgnoreCase);

    public IEnumerable<DictionaryEntry> ReadEntries(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);

        var header = ReadHeader(reader, path);
        if (header.Encrypted)
        {
            throw new DictionaryFormatException(
                "dictionary.import.encrypted_unsupported",
                $"{path} is an encrypted MDX file, which isn't supported.");
        }

        if (!header.Version.StartsWith("2.", StringComparison.Ordinal))
        {
            throw new DictionaryFormatException(
                "dictionary.import.unsupported_version",
                $"{path} is MDX version {header.Version}; only the 2.x block layout is supported.");
        }

        var keys = ReadKeyBlocks(reader, header, path);
        var recordStream = ReadRecordBlocks(reader, header, path);

        for (var i = 0; i < keys.Count; i++)
        {
            var (word, offset) = keys[i];
            var end = i + 1 < keys.Count ? keys[i + 1].Offset : recordStream.Length;
            if (offset < 0 || end > recordStream.Length || end < offset)
            {
                throw new DictionaryFormatException(
                    "dictionary.import.corrupt_index",
                    $"MDX record offset for '{word}' is out of range in {path}.");
            }

            var article = header.TextEncoding.GetString(recordStream, (int)offset, (int)(end - offset));
            yield return new DictionaryEntry(word, article);
        }
    }

    private sealed record MdxHeader(bool Encrypted, string Version, Encoding TextEncoding);

    private static MdxHeader ReadHeader(BinaryReader reader, string path)
    {
        var headerLength = ReadUInt32BE(reader);
        var headerBytes = reader.ReadBytes((int)headerLength);
        reader.ReadUInt32(); // header adler32 — not verified

        // The header text is null-terminated UTF-16LE XML; trim the trailing \0 before parsing.
        var headerXml = Encoding.Unicode.GetString(headerBytes).TrimEnd('\0');

        XElement root;
        try
        {
            root = XElement.Parse(headerXml);
        }
        catch (Exception ex)
        {
            throw new DictionaryFormatException(
                "dictionary.import.corrupt_header",
                $"{path}'s MDX header isn't valid XML: {ex.Message}");
        }

        var encrypted = root.Attribute("Encrypted")?.Value is { Length: > 0 } e && e != "0";
        var version = root.Attribute("RequiredEngineVersion")?.Value
            ?? root.Attribute("GeneratedByEngineVersion")?.Value
            ?? "1.2";
        var encodingName = root.Attribute("Encoding")?.Value;
        var encoding = encodingName is "UTF-16" or "UTF16" ? Encoding.Unicode : Encoding.UTF8;

        return new MdxHeader(encrypted, version, encoding);
    }

    private static List<(string Word, long Offset)> ReadKeyBlocks(BinaryReader reader, MdxHeader header, string path)
    {
        var numKeyBlocks = (long)ReadUInt64BE(reader);
        var numEntries = (long)ReadUInt64BE(reader);
        _ = ReadUInt64BE(reader); // key_block_info_decomp_size — not needed to parse sequentially
        var keyBlockInfoCompSize = (long)ReadUInt64BE(reader);
        _ = ReadUInt64BE(reader); // key_block_size_total
        reader.ReadUInt32(); // adler32 of the 40-byte header above — not verified

        var infoBlockBytes = reader.ReadBytes((int)keyBlockInfoCompSize);
        var infoDecomp = DecompressBlock(infoBlockBytes, path);

        var blockSizes = new List<(long CompSize, long DecompSize)>();
        var infoPos = 0;
        for (var i = 0; i < numKeyBlocks; i++)
        {
            infoPos += 8; // num_entries_in_block — unused here
            var firstLen = (int)ReadInt64BE(infoDecomp, infoPos); infoPos += 8;
            infoPos += firstLen;
            var lastLen = (int)ReadInt64BE(infoDecomp, infoPos); infoPos += 8;
            infoPos += lastLen;
            var compSize = ReadInt64BE(infoDecomp, infoPos); infoPos += 8;
            var decompSize = ReadInt64BE(infoDecomp, infoPos); infoPos += 8;
            blockSizes.Add((compSize, decompSize));
        }

        var keys = new List<(string, long)>((int)numEntries);
        foreach (var (compSize, _) in blockSizes)
        {
            var blockBytes = reader.ReadBytes((int)compSize);
            var decomp = DecompressBlock(blockBytes, path);

            var pos = 0;
            while (pos < decomp.Length)
            {
                var offset = ReadInt64BE(decomp, pos);
                pos += 8;
                var nullIndex = Array.IndexOf(decomp, (byte)0, pos);
                if (nullIndex < 0)
                {
                    nullIndex = decomp.Length;
                }

                var word = header.TextEncoding.GetString(decomp, pos, nullIndex - pos);
                pos = nullIndex + 1;
                keys.Add((word, offset));
            }
        }

        if (keys.Count != numEntries)
        {
            throw new DictionaryFormatException(
                "dictionary.import.corrupt_index",
                $"{path} declares {numEntries} MDX entries but {keys.Count} were parsed.");
        }

        return keys;
    }

    private static byte[] ReadRecordBlocks(BinaryReader reader, MdxHeader header, string path)
    {
        var numRecordBlocks = (long)ReadUInt64BE(reader);
        _ = ReadUInt64BE(reader); // num_entries — already validated against key count
        _ = ReadUInt64BE(reader); // record_block_info_size
        _ = ReadUInt64BE(reader); // record_block_size_total (on-disk, informational)

        var blockSizes = new List<(long CompSize, long DecompSize)>();
        for (var i = 0; i < numRecordBlocks; i++)
        {
            var compSize = (long)ReadUInt64BE(reader);
            var decompSize = (long)ReadUInt64BE(reader);
            blockSizes.Add((compSize, decompSize));
        }

        using var recordStream = new MemoryStream();
        foreach (var (compSize, _) in blockSizes)
        {
            var blockBytes = reader.ReadBytes((int)compSize);
            var decomp = DecompressBlock(blockBytes, path);
            recordStream.Write(decomp, 0, decomp.Length);
        }

        return recordStream.ToArray();
    }

    private static byte[] DecompressBlock(byte[] blockBytes, string path)
    {
        if (blockBytes.Length < 8)
        {
            throw new DictionaryFormatException("dictionary.import.corrupt_block", $"Truncated MDX block in {path}.");
        }

        var compType = blockBytes[0]; // low byte of the little-endian comp-type word
        var payload = blockBytes.AsSpan(8).ToArray(); // skip 4-byte comp-type + 4-byte adler32 (unverified)

        return compType switch
        {
            0 => payload,
            2 => InflateZlib(payload, path),
            1 => throw new DictionaryFormatException(
                "dictionary.import.lzo_unsupported",
                $"{path} uses LZO-compressed blocks, which aren't supported."),
            _ => throw new DictionaryFormatException(
                "dictionary.import.unsupported_compression",
                $"{path} uses an unrecognized MDX block compression type ({compType})."),
        };
    }

    private static byte[] InflateZlib(byte[] zlibPayload, string path)
    {
        try
        {
            // Skip the 2-byte zlib header; .NET's DeflateStream only understands raw deflate.
            using var input = new MemoryStream(zlibPayload, 2, zlibPayload.Length - 2);
            using var deflate = new DeflateStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            deflate.CopyTo(output);
            return output.ToArray();
        }
        catch (InvalidDataException ex)
        {
            throw new DictionaryFormatException(
                "dictionary.import.corrupt_block",
                $"Failed to inflate an MDX block in {path}: {ex.Message}");
        }
    }

    private static uint ReadUInt32BE(BinaryReader reader)
    {
        var bytes = reader.ReadBytes(4);
        return (uint)((bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3]);
    }

    private static ulong ReadUInt64BE(BinaryReader reader)
    {
        var bytes = reader.ReadBytes(8);
        ulong value = 0;
        foreach (var b in bytes)
        {
            value = (value << 8) | b;
        }

        return value;
    }

    private static long ReadInt64BE(byte[] buffer, int offset)
    {
        long value = 0;
        for (var i = 0; i < 8; i++)
        {
            value = (value << 8) | buffer[offset + i];
        }

        return value;
    }
}
