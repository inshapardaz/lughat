using System.Net;
using System.Security.Cryptography;
using Lughat.Engine.Api;
using Lughat.Engine.Api.Api;
using Lughat.Engine.Api.Auth;
using Lughat.Engine.Api.Data;
using Lughat.Engine.Api.Formats;
using Lughat.Engine.Api.Realtime;
using Lughat.Engine.Api.Search;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

var builder = WebApplication.CreateBuilder(args);

// Loopback only, ephemeral port — the shell reads the actual port back from the
// "READY:<port>" line below. See spec §2.
builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(IPAddress.Loopback, 0);
});

// In the real handshake the shell generates this token and passes it via env var when it
// spawns the sidecar. Falling back to a generated token lets the sidecar still run standalone
// (`dotnet run`) for manual testing.
var token = Environment.GetEnvironmentVariable("LUGHAT_ENGINE_TOKEN");
var tokenWasGenerated = string.IsNullOrEmpty(token);
if (tokenWasGenerated)
{
    token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
}

var appDataRoot = AppPaths.GetAppDataRoot();
var database = new AppDatabase(Path.Combine(appDataRoot, "db", "app.db"));
database.Migrate();

var providerRegistry = new DictionaryProviderRegistry()
    .Register(new StarDictProvider())
    .Register(new WordListProvider())
    .Register(new MdxProvider());

builder.Services.AddSingleton(providerRegistry);
builder.Services.AddSingleton(database);
builder.Services.AddSingleton<DictionaryRepository>();
builder.Services.AddSingleton<GroupRepository>();
builder.Services.AddSingleton<HistoryRepository>();
builder.Services.AddSingleton<FavoriteRepository>();
builder.Services.AddSingleton<SettingsRepository>();
builder.Services.AddSingleton(new IndexingService(Path.Combine(appDataRoot, "index")));
builder.Services.AddSingleton<SearchService>();
builder.Services.AddSingleton<EventHub>();
builder.Services.AddSingleton<DictionaryImportService>();

// Source-gen only, no reflection fallback — see AppJsonContext's doc comment. This makes a
// missing-type mistake fail immediately in ordinary `dotnet run`, not just in a trimmed
// publish.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolver = AppJsonContext.Default;
});

var app = builder.Build();

app.UseMiddleware<BearerTokenMiddleware>(token!);
app.UseWebSockets();

app.MapDictionaryEndpoints();
app.MapLookupEndpoints();
app.MapMediaEndpoints();
app.MapSettingsEndpoints();

app.MapGet("/api/ping", () => Results.Ok(new PingResponse("ok", "Lughat.Engine.Api")));

app.MapPost("/api/shutdown", (IHostApplicationLifetime lifetime) =>
{
    lifetime.StopApplication();
    return Results.NoContent();
});

app.Map("/ws", async (HttpContext context, EventHub eventHub) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    await eventHub.HandleConnectionAsync(socket, context.RequestAborted);
});

app.Start();

var addressesFeature = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
var boundPort = new Uri(addressesFeature!.Addresses.First()).Port;

Console.WriteLine($"READY:{boundPort}");
if (tokenWasGenerated)
{
    Console.WriteLine($"TOKEN:{token}");
}

await app.WaitForShutdownAsync();
