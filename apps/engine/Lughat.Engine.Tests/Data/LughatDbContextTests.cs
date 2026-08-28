using Microsoft.EntityFrameworkCore;

namespace Lughat.Engine.Tests.Data;

public class LughatDbContextTests
{
    [Fact]
    public void Migrate_creates_all_tables()
    {
        var dbPath = TestDb.NewTempDbPath();
        try
        {
            using var db = TestDb.CreateMigrated(dbPath);

            var tableNames = db.Database
                .SqlQueryRaw<string>("SELECT name FROM sqlite_master WHERE type = 'table'")
                .ToList();

            Assert.Contains("Dictionaries", tableNames);
            Assert.Contains("Groups", tableNames);
            Assert.Contains("History", tableNames);
            Assert.Contains("Favorites", tableNames);
            Assert.Contains("Settings", tableNames);
        }
        finally
        {
            File.Delete(dbPath);
        }
    }

    [Fact]
    public void Migrate_is_idempotent()
    {
        var dbPath = TestDb.NewTempDbPath();
        try
        {
            using (var db = TestDb.CreateMigrated(dbPath))
            {
                db.Database.Migrate(); // second call on the already-migrated db should not throw
            }
        }
        finally
        {
            File.Delete(dbPath);
        }
    }
}
