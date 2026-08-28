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
    EventHub eventHub)
{
    public DictionaryRecord Import(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new DictionaryFormatException(ErrorCodes.SourceFileMissing, $"{filePath} does not exist.");
        }

        var provider = providers.FindProviderForPath(filePath)
            ?? throw new DictionaryFormatException(ErrorCodes.UnsupportedFormat, $"No provider recognizes {filePath}.");

        var contentHash = IndexingService.ComputeContentHash(filePath);
        var name = Path.GetFileNameWithoutExtension(filePath);
        var record = dictionaryRepository.Insert(name, provider.FormatId, filePath, contentHash);

        _ = IndexInBackgroundAsync(record, provider, contentHash);
        return record;
    }

    private async Task IndexInBackgroundAsync(DictionaryRecord record, IDictionaryProvider provider, string contentHash)
    {
        try
        {
            if (!indexingService.IsIndexed(contentHash))
            {
                await Task.Run(() => indexingService.BuildIndex(
                    record.Id,
                    contentHash,
                    provider.ReadEntries(record.FilePath),
                    onProgress: e => _ = eventHub.BroadcastAsync(new EngineEventMessage(
                        Type: e.Complete ? "index-complete" : "index-progress",
                        DictId: e.DictionaryId,
                        Percent: e.Percent))));
            }
            else
            {
                await eventHub.BroadcastAsync(new EngineEventMessage("index-complete", record.Id, Percent: 100));
            }

            dictionaryRepository.MarkIndexed(record.Id, DateTimeOffset.UtcNow.ToString("O"));
        }
        catch (DictionaryFormatException ex)
        {
            await eventHub.BroadcastAsync(new EngineEventMessage("index-error", record.Id, Error: ex.ErrorCode));
        }
    }
}
