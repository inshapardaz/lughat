using Microsoft.EntityFrameworkCore;

namespace Lughat.Engine.Api.Data;

public sealed class DictionaryRepository(LughatDbContext db)
{
    public IReadOnlyList<DictionaryEntity> List() =>
        db.Dictionaries.AsNoTracking().OrderBy(d => d.GroupId).ThenBy(d => d.SortOrder).ToList();

    public DictionaryEntity? Find(string id) =>
        db.Dictionaries.AsNoTracking().FirstOrDefault(d => d.Id == id);

    public DictionaryEntity Insert(string name, string format, string filePath, string contentHash, string language = "en")
    {
        var nextSortOrder = (db.Dictionaries.Select(d => (int?)d.SortOrder).Max() ?? -1) + 1;

        var entity = new DictionaryEntity
        {
            Id = Guid.NewGuid().ToString("n"),
            Name = name,
            Format = format,
            FilePath = filePath,
            ContentHash = contentHash,
            Enabled = true,
            GroupId = null,
            SortOrder = nextSortOrder,
            IndexedAt = null,
            Language = language,
        };

        db.Dictionaries.Add(entity);
        db.SaveChanges();
        return entity;
    }

    public void MarkIndexed(string id, string indexedAtIso)
    {
        var entity = db.Dictionaries.FirstOrDefault(d => d.Id == id);
        if (entity is null)
        {
            return;
        }

        entity.IndexedAt = indexedAtIso;
        db.SaveChanges();
    }

    public void SetEnabled(string id, bool enabled)
    {
        var entity = db.Dictionaries.FirstOrDefault(d => d.Id == id);
        if (entity is null)
        {
            return;
        }

        entity.Enabled = enabled;
        db.SaveChanges();
    }

    public void UpdateOrder(string id, string? groupId, int sortOrder)
    {
        var entity = db.Dictionaries.FirstOrDefault(d => d.Id == id);
        if (entity is null)
        {
            return;
        }

        entity.GroupId = groupId;
        entity.SortOrder = sortOrder;
        db.SaveChanges();
    }

    public bool Delete(string id)
    {
        var entity = db.Dictionaries.FirstOrDefault(d => d.Id == id);
        if (entity is null)
        {
            return false;
        }

        // History/Favorites rows for this dictionary cascade-delete — see LughatDbContext's
        // OnModelCreating; they're meaningless without the dictionary they reference.
        db.Dictionaries.Remove(entity);
        db.SaveChanges();
        return true;
    }
}

public sealed class GroupRepository(LughatDbContext db)
{
    public IReadOnlyList<GroupEntity> List() =>
        db.Groups.AsNoTracking().OrderBy(g => g.SortOrder).ToList();

    public GroupEntity Create(string name)
    {
        var nextSortOrder = (db.Groups.Select(g => (int?)g.SortOrder).Max() ?? -1) + 1;
        var entity = new GroupEntity { Id = Guid.NewGuid().ToString("n"), Name = name, SortOrder = nextSortOrder };
        db.Groups.Add(entity);
        db.SaveChanges();
        return entity;
    }
}

public sealed class HistoryRepository(LughatDbContext db)
{
    public void Record(string term, string dictionaryId, string timestampIso)
    {
        db.History.Add(new HistoryEntity
        {
            Id = Guid.NewGuid().ToString("n"),
            Term = term,
            DictionaryId = dictionaryId,
            Timestamp = timestampIso,
        });
        db.SaveChanges();
    }

    public IReadOnlyList<HistoryEntity> Recent(int limit = 50) =>
        db.History.AsNoTracking().OrderByDescending(h => h.Timestamp).Take(limit).ToList();
}

public sealed class FavoriteRepository(LughatDbContext db)
{
    public FavoriteEntity Add(string term, string dictionaryId, string? tag, string createdAtIso)
    {
        var entity = new FavoriteEntity
        {
            Id = Guid.NewGuid().ToString("n"),
            Term = term,
            DictionaryId = dictionaryId,
            Tag = tag,
            CreatedAt = createdAtIso,
        };

        db.Favorites.Add(entity);
        db.SaveChanges();
        return entity;
    }

    public IReadOnlyList<FavoriteEntity> List() =>
        db.Favorites.AsNoTracking().OrderByDescending(f => f.CreatedAt).ToList();

    public bool Remove(string id)
    {
        var entity = db.Favorites.FirstOrDefault(f => f.Id == id);
        if (entity is null)
        {
            return false;
        }

        db.Favorites.Remove(entity);
        db.SaveChanges();
        return true;
    }
}

public sealed class SettingsRepository(LughatDbContext db)
{
    public string? Get(string key) =>
        db.Settings.AsNoTracking().FirstOrDefault(s => s.Key == key)?.ValueJson;

    public void Set(string key, string valueJson)
    {
        var existing = db.Settings.FirstOrDefault(s => s.Key == key);
        if (existing is null)
        {
            db.Settings.Add(new SettingEntity { Key = key, ValueJson = valueJson });
        }
        else
        {
            existing.ValueJson = valueJson;
        }

        db.SaveChanges();
    }
}
