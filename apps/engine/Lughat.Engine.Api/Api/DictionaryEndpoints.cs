using Lughat.Engine.Api.Data;
using Lughat.Engine.Api.Formats;

namespace Lughat.Engine.Api.Api;

public static class DictionaryEndpoints
{
    public static void MapDictionaryEndpoints(this WebApplication app)
    {
        app.MapGet("/api/dictionaries", (DictionaryRepository dictionaries, GroupRepository groups) =>
            Results.Ok(new DictionariesResponse(dictionaries.List(), groups.List())));

        app.MapPost("/api/dictionaries", (ImportRequest request, DictionaryImportService importService) =>
        {
            try
            {
                return Results.Ok(importService.Import(request.Path));
            }
            catch (DictionaryFormatException ex)
            {
                return ErrorCodes.Problem(ex.ErrorCode, ex.Message);
            }
        });

        app.MapDelete("/api/dictionaries/{id}", (string id, DictionaryRepository dictionaries) =>
            dictionaries.Delete(id)
                ? Results.NoContent()
                : ErrorCodes.Problem(ErrorCodes.DictionaryNotFound, $"No dictionary with id {id}.", StatusCodes.Status404NotFound));

        app.MapPut("/api/dictionaries/{id}/order", (string id, OrderRequest request, DictionaryRepository dictionaries) =>
        {
            dictionaries.UpdateOrder(id, request.GroupId, request.SortOrder);
            return Results.NoContent();
        });

        app.MapPut("/api/dictionaries/{id}/enabled", (string id, EnabledRequest request, DictionaryRepository dictionaries) =>
        {
            dictionaries.SetEnabled(id, request.Enabled);
            return Results.NoContent();
        });

        app.MapGet("/api/groups", (GroupRepository groups) => Results.Ok(groups.List()));

        app.MapPost("/api/groups", (GroupRequest request, GroupRepository groups) =>
            Results.Ok(groups.Create(request.Name)));
    }

    public sealed record ImportRequest(string Path);

    public sealed record OrderRequest(string? GroupId, int SortOrder);

    public sealed record EnabledRequest(bool Enabled);

    public sealed record GroupRequest(string Name);
}
