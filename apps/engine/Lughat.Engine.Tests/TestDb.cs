using Lughat.Engine.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Lughat.Engine.Tests;

internal static class TestDb
{
    public static string NewTempDbPath() =>
        Path.Combine(Path.GetTempPath(), "lughat-tests-" + Guid.NewGuid().ToString("n") + ".db");

    public static LughatDbContext CreateMigrated(string dbPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        var options = new DbContextOptionsBuilder<LughatDbContext>()
            .UseSqlite($"Data Source={dbPath};Pooling=False")
            .Options;
        var context = new LughatDbContext(options);
        context.Database.Migrate();
        return context;
    }
}
