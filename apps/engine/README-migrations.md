# Database migrations

The engine's SQLite schema (spec §7: Dictionaries, Groups, History, Favorites, Settings)
is managed by EF Core migrations under `Lughat.Engine.Api/Data/Migrations/`. `Program.cs`
applies any pending migrations automatically at startup (`Database.Migrate()`) — there's no
manual step for a normal run or a packaged build.

## Changing the schema

1. Edit the entity classes in `Data/Entities.cs` and/or `LughatDbContext.OnModelCreating`.
2. Generate a migration from the repo's `apps/engine` directory:

   ```
   dotnet tool run dotnet-ef migrations add <DescriptiveName> \
     --project Lughat.Engine.Api/Lughat.Engine.Api.csproj \
     --startup-project Lughat.Engine.Api/Lughat.Engine.Api.csproj \
     -o Data/Migrations
   ```

   (`dotnet-ef` is a local tool — restore it once with `dotnet tool restore` if it's not on
   your PATH.)
3. Read the generated migration under `Data/Migrations/` before committing it — EF Core's
   diff is usually right, but it's the actual SQL that will run against everyone's existing
   database, so it's worth a look, especially for anything destructive (dropping/renaming a
   column loses data; EF Core won't warn you).
4. Commit the migration files alongside the entity changes that produced them, in the same
   change. `LughatDbContextModelSnapshot.cs` is regenerated each time — always commit the
   version `migrations add` produced, don't hand-edit it.

## Design-time database

`LughatDbContextFactory` (`IDesignTimeDbContextFactory<LughatDbContext>`) is what lets the
`dotnet-ef` CLI construct a `DbContext` without running the full app — Program.cs's
top-level statements aren't something the tooling can drive directly, and the real
connection string depends on `AppPaths.GetAppDataRoot()`, which isn't meaningful at design
time anyway. The factory points at a throwaway `design-time.db` (gitignored) purely so the
tooling has *a* SQLite provider to diff against; it's never actually opened for schema-add
operations and safe to delete if it ever appears.
