using Microsoft.EntityFrameworkCore;

namespace Lughat.Engine.Api.Data;

/// <summary>
/// EF Core replaces the earlier hand-rolled PRAGMA user_version migration scheme (spec §7)
/// with real, generated migrations under Migrations/ — see apps/engine/README-migrations.md
/// for the day-to-day workflow. Registered scoped (EF Core's default and requirement — a
/// DbContext isn't thread-safe for concurrent use), which is why the repositories and
/// anything that depends on them are scoped too; see Program.cs's service registrations.
/// </summary>
public sealed class LughatDbContext(DbContextOptions<LughatDbContext> options) : DbContext(options)
{
    public DbSet<DictionaryEntity> Dictionaries => Set<DictionaryEntity>();
    public DbSet<GroupEntity> Groups => Set<GroupEntity>();
    public DbSet<HistoryEntity> History => Set<HistoryEntity>();
    public DbSet<FavoriteEntity> Favorites => Set<FavoriteEntity>();
    public DbSet<SettingEntity> Settings => Set<SettingEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DictionaryEntity>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.HasOne<GroupEntity>()
                .WithMany()
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<GroupEntity>().HasKey(g => g.Id);

        modelBuilder.Entity<HistoryEntity>(entity =>
        {
            entity.HasKey(h => h.Id);
            entity.HasOne<DictionaryEntity>()
                .WithMany()
                .HasForeignKey(h => h.DictionaryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FavoriteEntity>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.HasOne<DictionaryEntity>()
                .WithMany()
                .HasForeignKey(f => f.DictionaryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SettingEntity>().HasKey(s => s.Key);
    }
}
