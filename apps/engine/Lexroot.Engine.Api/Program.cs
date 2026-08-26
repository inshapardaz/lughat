using System.Net;
using System.Security.Cryptography;
using Lexroot.Engine.Api.Auth;
using Lexroot.Engine.Api.Formats;
using Lexroot.Engine.Api.Search;
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
// (`dotnet run`) for manual testing, per the Phase 0 acceptance criteria.
var token = Environment.GetEnvironmentVariable("LEXROOT_ENGINE_TOKEN");
var tokenWasGenerated = string.IsNullOrEmpty(token);
if (tokenWasGenerated)
{
    token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
}

var app = builder.Build();

app.UseMiddleware<BearerTokenMiddleware>(token!);

app.MapGet("/api/ping", () => Results.Ok(new { status = "ok", service = "Lexroot.Engine.Api" }));

var indexService = new LuceneIndexService();
var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "spike-dict", "spike-dict.ifo");
if (File.Exists(fixturePath))
{
    indexService.Build(StarDictReader.Read(fixturePath));
}

app.MapGet("/api/lookup", (string term, string? mode) =>
    Results.Ok(indexService.Lookup(term, mode ?? "exact")));

app.Start();

var addressesFeature = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
var boundPort = new Uri(addressesFeature!.Addresses.First()).Port;

Console.WriteLine($"READY:{boundPort}");
if (tokenWasGenerated)
{
    Console.WriteLine($"TOKEN:{token}");
}

await app.WaitForShutdownAsync();
