using Lughat.Engine.Api.Data;

namespace Lughat.Engine.Api.Api;

public static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this WebApplication app)
    {
        app.MapGet("/api/settings/{key}", (string key, SettingsRepository settings) =>
        {
            var value = settings.Get(key);
            return value is null ? Results.NoContent() : Results.Text(value, "application/json");
        });

        app.MapPut("/api/settings/{key}", async (string key, HttpRequest request, SettingsRepository settings) =>
        {
            using var reader = new StreamReader(request.Body);
            var valueJson = await reader.ReadToEndAsync();
            settings.Set(key, valueJson);
            return Results.NoContent();
        });
    }
}
