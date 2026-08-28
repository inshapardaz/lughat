using Lughat.Engine.Api.Data;
using Lughat.Engine.Api.Search;

namespace Lughat.Engine.Api.Api;

public static class LookupEndpoints
{
    public static void MapLookupEndpoints(this WebApplication app)
    {
        app.MapGet("/api/lookup", (string term, string? dictionaryIds, SearchService search, HistoryRepository history) =>
        {
            var results = search.Search(ParseIds(dictionaryIds), term, "exact");
            if (results.Count > 0)
            {
                history.Record(term, results[0].DictionaryId, DateTimeOffset.UtcNow.ToString("O"));
            }

            return Results.Ok(results);
        });

        app.MapGet("/api/search", (string query, string? mode, string? dictionaryIds, SearchService search) =>
            Results.Ok(search.Search(ParseIds(dictionaryIds), query, mode ?? "prefix")));

        app.MapGet("/api/history", (HistoryRepository history) => Results.Ok(history.Recent()));

        app.MapGet("/api/favorites", (FavoriteRepository favorites) => Results.Ok(favorites.List()));

        app.MapPost("/api/favorites", (FavoriteRequest request, FavoriteRepository favorites) =>
            Results.Ok(favorites.Add(request.Term, request.DictionaryId, request.Tag, DateTimeOffset.UtcNow.ToString("O"))));

        app.MapDelete("/api/favorites/{id}", (string id, FavoriteRepository favorites) =>
            favorites.Remove(id)
                ? Results.NoContent()
                : ErrorCodes.Problem(ErrorCodes.FavoriteNotFound, $"No favorite with id {id}.", StatusCodes.Status404NotFound));
    }

    private static string[]? ParseIds(string? csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? null
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public sealed record FavoriteRequest(string Term, string DictionaryId, string? Tag);
}
