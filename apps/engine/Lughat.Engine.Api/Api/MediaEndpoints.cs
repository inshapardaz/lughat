using Lughat.Engine.Api.Data;

namespace Lughat.Engine.Api.Api;

public static class MediaEndpoints
{
    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".svg"] = "image/svg+xml",
        [".mp3"] = "audio/mpeg",
        [".wav"] = "audio/wav",
        [".ogg"] = "audio/ogg",
    };

    /// <summary>
    /// Serves media referenced by a dictionary's articles. Supports StarDict's <c>res/</c>
    /// folder convention (media files sitting next to the .ifo, referenced by relative
    /// path) — extracting binary resources embedded directly inside other formats' data
    /// files is a known gap, noted next to the providers that skip them.
    /// </summary>
    public static void MapMediaEndpoints(this WebApplication app)
    {
        app.MapGet("/api/media/{dictId}/{*path}", (string dictId, string path, DictionaryRepository dictionaries) =>
        {
            var dict = dictionaries.Find(dictId);
            if (dict is null)
            {
                return ErrorCodes.Problem(ErrorCodes.DictionaryNotFound, $"No dictionary with id {dictId}.", StatusCodes.Status404NotFound);
            }

            var dictDir = Path.GetDirectoryName(dict.FilePath)!;
            var resRoot = Path.GetFullPath(Path.Combine(dictDir, "res")) + Path.DirectorySeparatorChar;
            var resolved = Path.GetFullPath(Path.Combine(dictDir, "res", path));

            if (!resolved.StartsWith(resRoot, StringComparison.Ordinal) || !File.Exists(resolved))
            {
                return ErrorCodes.Problem(ErrorCodes.MediaNotFound, $"No media at {path} for dictionary {dictId}.", StatusCodes.Status404NotFound);
            }

            var contentType = ContentTypes.GetValueOrDefault(Path.GetExtension(resolved), "application/octet-stream");
            return Results.File(resolved, contentType);
        });
    }
}
