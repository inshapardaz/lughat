namespace Lughat.Engine.Api.Api;

public sealed record PingResponse(string Status, string Service);

public sealed record ErrorResponse(string Error, string Detail);

public sealed record DictionariesResponse(
    IReadOnlyList<Data.DictionaryEntity> Dictionaries,
    IReadOnlyList<Data.GroupEntity> Groups);
