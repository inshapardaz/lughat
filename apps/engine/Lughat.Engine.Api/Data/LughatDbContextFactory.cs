using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lughat.Engine.Api.Data;

/// <summary>
/// Lets `dotnet ef migrations add` / `database update` construct a DbContext without
/// running the full app — Program.cs's top-level statements aren't something EF's tooling
/// can drive directly. The connection string here only matters for design-time schema
/// diffing; the real path comes from AppPaths.GetAppDataRoot() in Program.cs at runtime.
/// </summary>
public sealed class LughatDbContextFactory : IDesignTimeDbContextFactory<LughatDbContext>
{
    public LughatDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LughatDbContext>();
        optionsBuilder.UseSqlite("Data Source=design-time.db");
        return new LughatDbContext(optionsBuilder.Options);
    }
}
