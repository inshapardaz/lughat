using Dapper;
using Lughat.Engine.Api.Data;

namespace Lughat.Engine.Tests.Data;

public class AppDatabaseTests
{
    [Fact]
    public void Migrate_creates_all_tables()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), "lughat-tests-" + Guid.NewGuid().ToString("n") + ".db");
        try
        {
            var database = new AppDatabase(dbPath);
            database.Migrate();

            using var connection = database.OpenConnection();
            var tableNames = connection.Query<string>(
                "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name").AsList();

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
        var dbPath = Path.Combine(Path.GetTempPath(), "lughat-tests-" + Guid.NewGuid().ToString("n") + ".db");
        try
        {
            var database = new AppDatabase(dbPath);
            database.Migrate();
            database.Migrate(); // should not throw or attempt to recreate existing tables
        }
        finally
        {
            File.Delete(dbPath);
        }
    }
}
