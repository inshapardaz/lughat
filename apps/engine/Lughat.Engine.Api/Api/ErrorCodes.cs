namespace Lughat.Engine.Api.Api;

/// <summary>
/// The engine never returns human-readable error text — only stable codes the renderer
/// maps to a localized message (spec §9's localisation boundary). Codes raised by format
/// providers themselves (e.g. <c>dictionary.import.corrupt_index</c>) live next to those
/// providers in Formats/*.cs; this class covers codes the API layer raises directly.
/// </summary>
public static class ErrorCodes
{
    public const string DictionaryNotFound = "dictionary.not_found";
    public const string FavoriteNotFound = "favorite.not_found";
    public const string UnsupportedFormat = "dictionary.import.unsupported_format";
    public const string SourceFileMissing = "dictionary.import.source_missing";
    public const string MediaNotFound = "media.not_found";

    public static IResult Problem(string code, string detail, int statusCode = StatusCodes.Status400BadRequest) =>
        Results.Json(new ErrorResponse(code, detail), AppJsonContext.Default.ErrorResponse, statusCode: statusCode);
}
