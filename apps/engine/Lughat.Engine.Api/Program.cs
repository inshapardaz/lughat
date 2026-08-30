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
using Microsoft.EntityFrameworkCore;

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
Directory.CreateDirectory(Path.Combine(appDataRoot, "db"));

var providerRegistry = new DictionaryProviderRegistry()
    .Register(new StarDictProvider())
    .Register(new WordListProvider())
    .Register(new MdxProvider())
    .Register(new DslProvider())
    .Register(new XdxfProvider())
    .Register(new WordNetProvider())
    .Register(new KaikkiProvider());

builder.Services.AddSingleton(providerRegistry);

// EF Core's DbContext isn't thread-safe for concurrent use, so it (and anything that
// depends on it) is registered scoped, not singleton — one instance per HTTP request.
// DictionaryImportService's background indexing work creates its own scope explicitly,
// since it outlives the request that started it — see its doc comment.
builder.Services.AddDbContext<LughatDbContext>(options =>
    options.UseSqlite($"Data Source={Path.Combine(appDataRoot, "db", "app.db")}"));
builder.Services.AddScoped<DictionaryRepository>();
builder.Services.AddScoped<GroupRepository>();
builder.Services.AddScoped<HistoryRepository>();
builder.Services.AddScoped<FavoriteRepository>();
builder.Services.AddScoped<SettingsRepository>();
builder.Services.AddScoped<SearchService>();
builder.Services.AddScoped<DictionaryImportService>();

builder.Services.AddSingleton(new IndexingService(Path.Combine(appDataRoot, "index")));
builder.Services.AddSingleton<EventHub>();

// The renderer calls this API cross-origin — the Vite dev server (http://localhost:5173) and
// a packaged Electron window (file://, which browsers send as Origin: null) are both a
// different origin than the engine's own loopback port. Every one of those fetches also
// carries an Authorization header, which is enough on its own to force a CORS preflight
// (OPTIONS) even for a plain GET. Access is already gated by the per-launch bearer token
// (see BearerTokenMiddleware) and this only ever binds to loopback, so there's no origin
// allowlist worth maintaining here — AllowAnyOrigin is the actual security boundary-neutral
// choice, not a shortcut past one.
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// Source-gen only, no reflection fallback — see AppJsonContext's doc comment. This makes a
// missing-type mistake fail immediately in ordinary `dotnet run`, not just in a trimmed
// publish.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolver = AppJsonContext.Default;
});

var app = builder.Build();

using (var migrationScope = app.Services.CreateScope())
{
    migrationScope.ServiceProvider.GetRequiredService<LughatDbContext>().Database.Migrate();
}

// Fresh install → no dictionaries yet → import the bundled starter dictionary (issue #59) so
// the app is immediately useful rather than opening to an empty shelf. Best-effort: a missing
// or malformed bundled file shouldn't stop the app from starting.
using (var bootstrapScope = app.Services.CreateScope())
{
    var dictionaries = bootstrapScope.ServiceProvider.GetRequiredService<DictionaryRepository>();
    if (dictionaries.List().Count == 0)
    {
        var starterPath = Path.Combine(AppContext.BaseDirectory, "StarterDictionary", "starter-en.jsonl");
        if (File.Exists(starterPath))
        {
            try
            {
                bootstrapScope.ServiceProvider.GetRequiredService<DictionaryImportService>().Import(starterPath, "en");
            }
            catch (DictionaryFormatException)
            {
                // Starter dictionary is a nice-to-have, not load-bearing — swallow and move on.
            }
        }
    }
}

// Must run before the bearer token check: a CORS preflight (OPTIONS) request never carries
// the Authorization header by spec, so if this ran after the auth middleware every preflight
// would get a 401 and the browser would report it as a CORS failure on the real request —
// exactly the bug this fixes. UseCors() answers preflight requests itself and never forwards
// them further down the pipeline, so BearerTokenMiddleware only ever sees the real request.
app.UseCors();
app.UseMiddleware<BearerTokenMiddleware>(token!);
app.UseWebSockets();

app.MapDictionaryEndpoints();
app.MapLookupEndpoints();
app.MapMediaEndpoints();
app.MapSettingsEndpoints();
app.MapAnkiEndpoints();

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
