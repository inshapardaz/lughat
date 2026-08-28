using System.Text.Json.Serialization;
using Lughat.Engine.Api.Data;
using Lughat.Engine.Api.Realtime;
using Lughat.Engine.Api.Search;

namespace Lughat.Engine.Api.Api;

/// <summary>
/// Source-generated JSON metadata for every type that crosses an HTTP boundary. Required
/// for the trimmed self-contained publish (spec §12 / "Sidecar self-contained trimmed
/// publish" issue): System.Text.Json's reflection-based serializer silently loses type
/// metadata under trimming, which doesn't just break the type in question — it breaks
/// Minimal API's endpoint routing table entirely, so even unrelated endpoints 500. Program.cs
/// sets this as the *only* JsonSerializerOptions.TypeInfoResolver (no reflection fallback),
/// so a type missing from this list fails immediately in ordinary `dotnet run` too, rather
/// than only surfacing in a slow trimmed-publish test cycle.
/// </summary>
[JsonSerializable(typeof(DictionaryRecord))]
[JsonSerializable(typeof(IReadOnlyList<DictionaryRecord>))]
[JsonSerializable(typeof(GroupRecord))]
[JsonSerializable(typeof(IReadOnlyList<GroupRecord>))]
[JsonSerializable(typeof(HistoryRecord))]
[JsonSerializable(typeof(IReadOnlyList<HistoryRecord>))]
[JsonSerializable(typeof(FavoriteRecord))]
[JsonSerializable(typeof(IReadOnlyList<FavoriteRecord>))]
[JsonSerializable(typeof(SearchHit))]
[JsonSerializable(typeof(IReadOnlyList<SearchHit>))]
[JsonSerializable(typeof(DictionaryEndpoints.ImportRequest))]
[JsonSerializable(typeof(DictionaryEndpoints.OrderRequest))]
[JsonSerializable(typeof(DictionaryEndpoints.EnabledRequest))]
[JsonSerializable(typeof(DictionaryEndpoints.GroupRequest))]
[JsonSerializable(typeof(LookupEndpoints.FavoriteRequest))]
[JsonSerializable(typeof(DictionariesResponse))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(PingResponse))]
[JsonSerializable(typeof(EngineEventMessage))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class AppJsonContext : JsonSerializerContext;
