using System.Text.Json.Serialization;
using Lughat.Engine.Api.Data;
using Lughat.Engine.Api.Realtime;
using Lughat.Engine.Api.Search;

namespace Lughat.Engine.Api.Api;

/// <summary>
/// Source-generated JSON metadata for every type that crosses an HTTP boundary. Originally
/// added because System.Text.Json's reflection-based serializer breaks Minimal API's entire
/// routing table under IL trimming (see apps/engine/publish.sh — trimming is off now, for
/// unrelated Dapper/EF reasons, but this is still the right approach regardless: Program.cs
/// sets it as the *only* JsonSerializerOptions.TypeInfoResolver, no reflection fallback, so
/// a type missing from this list fails immediately in ordinary `dotnet run` rather than only
/// surfacing later.
/// </summary>
[JsonSerializable(typeof(DictionaryEntity))]
[JsonSerializable(typeof(IReadOnlyList<DictionaryEntity>))]
[JsonSerializable(typeof(GroupEntity))]
[JsonSerializable(typeof(IReadOnlyList<GroupEntity>))]
[JsonSerializable(typeof(HistoryEntity))]
[JsonSerializable(typeof(IReadOnlyList<HistoryEntity>))]
[JsonSerializable(typeof(FavoriteEntity))]
[JsonSerializable(typeof(IReadOnlyList<FavoriteEntity>))]
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
[JsonSerializable(typeof(AnkiEndpoints.AnkiExportRequest))]
[JsonSerializable(typeof(AnkiEndpoints.AnkiExportCardRequest))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class AppJsonContext : JsonSerializerContext;
