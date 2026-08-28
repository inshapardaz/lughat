namespace Lughat.Engine.Api.Api;

public static class AnkiEndpoints
{
    public static void MapAnkiEndpoints(this WebApplication app)
    {
        app.MapPost("/api/anki-export", (AnkiExportRequest request) =>
        {
            if (request.Cards.Count == 0)
            {
                return ErrorCodes.Problem("anki.export.no_cards", "At least one card is required.");
            }

            var cards = request.Cards.Select(c => new AnkiCard(c.Front, c.Back)).ToList();
            var bytes = AnkiExportService.BuildPackage(request.DeckName, cards);
            return Results.File(bytes, "application/octet-stream", "lughat-export.apkg");
        });
    }

    public sealed record AnkiExportRequest(string DeckName, IReadOnlyList<AnkiExportCardRequest> Cards);

    public sealed record AnkiExportCardRequest(string Front, string Back);
}
