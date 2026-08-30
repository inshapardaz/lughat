using Lughat.Engine.Api.Data;
using Lughat.Engine.Api.Formats;
using Lughat.Engine.Api.Realtime;
using Lughat.Engine.Api.Search;

namespace Lughat.Engine.Api.Api;

/// <summary>
/// Ties format detection, content hashing, and indexing together for a dictionary import.
/// Indexing runs in the background so the import call returns immediately; progress and
/// completion are pushed over <see cref="EventHub"/> (spec §8 / §9).
/// </summary>
public sealed class DictionaryImportService(
    DictionaryProviderRegistry providers,
    DictionaryRepository dictionaryRepository,
    IndexingService indexingService,
    EventHub eventHub,
    IServiceScopeFactory scopeFactory)
{
    public DictionaryEntity Import(string filePath, string language = "en")
    {
        if (!File.Exists(filePath))
        {
            throw new DictionaryFormatException(ErrorCodes.SourceFileMissing, $"{filePath} does not exist.");
        }

        var provider = providers.FindProviderForPath(filePath)
            ?? throw new DictionaryFormatException(ErrorCodes.UnsupportedFormat, $"No provider recognizes {filePath}.");

        var contentHash = IndexingService.ComputeContentHash(filePath);
        var name = Path.GetFileNameWithoutExtension(filePath);
        // Uses the request-scoped repository directly — this part runs synchronously within
        // the HTTP request that's importing the dictionary.
        var entity = dictionaryRepository.Insert(name, provider.FormatId, filePath, contentHash, language);

        _ = IndexInBackgroundAsync(entity, provider, contentHash);
        return entity;
    }

    private async Task IndexInBackgroundAsync(DictionaryEntity entity, IDictionaryProvider provider, string contentHash)
    {
        // Indexing outlives the HTTP request that triggered it, so it can't use the
        // request's DbContext (scoped services get disposed when the request ends) — this
        // creates its own scope, independent of whatever scope Import() was called from.
        using var scope = scopeFactory.CreateScope();
        var scopedDictionaries = scope.ServiceProvider.GetRequiredService<DictionaryRepository>();

        try
        {
            if (!indexingService.IsIndexed(contentHash))
            {
                await Task.Run(() => indexingService.BuildIndex(
                    entity.Id,
                    contentHash,
                    provider.ReadEntries(entity.FilePath),
                    entity.Language,
                    onProgress: e => _ = eventHub.BroadcastAsync(new EngineEventMessage(
                        Type: e.Complete ? "index-complete" : "index-progress",
                        DictId: e.DictionaryId,
                        Percent: e.Percent))));
            }
            else
            {
                await eventHub.BroadcastAsync(new EngineEventMessage("index-complete", entity.Id, Percent: 100));
            }

            scopedDictionaries.MarkIndexed(entity.Id, DateTimeOffset.UtcNow.ToString("O"));
        }
        catch (DictionaryFormatException ex)
        {
            await eventHub.BroadcastAsync(new EngineEventMessage("index-error", entity.Id, Error: ex.ErrorCode));
        }
    }
}
