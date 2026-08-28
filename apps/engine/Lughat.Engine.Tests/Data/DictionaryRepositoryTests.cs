using Lughat.Engine.Api.Data;

namespace Lughat.Engine.Tests.Data;

public class DictionaryRepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly LughatDbContext _db;
    private readonly DictionaryRepository _repository;
    private readonly GroupRepository _groups;

    public DictionaryRepositoryTests()
    {
        _dbPath = TestDb.NewTempDbPath();
        _db = TestDb.CreateMigrated(_dbPath);
        _repository = new DictionaryRepository(_db);
        _groups = new GroupRepository(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        File.Delete(_dbPath);
    }

    [Fact]
    public void Insert_then_List_round_trips_all_fields()
    {
        var inserted = _repository.Insert("My Dict", "stardict", "C:/dict.ifo", "abc123");

        var listed = Assert.Single(_repository.List());
        Assert.Equal(inserted.Id, listed.Id);
        Assert.Equal("My Dict", listed.Name);
        Assert.True(listed.Enabled);
        Assert.Null(listed.IndexedAt);
    }

    [Fact]
    public void SetEnabled_and_UpdateOrder_persist()
    {
        var record = _repository.Insert("My Dict", "stardict", "C:/dict.ifo", "abc123");
        var group = _groups.Create("English");

        _repository.SetEnabled(record.Id, false);
        _repository.UpdateOrder(record.Id, group.Id, 5);

        var updated = _repository.Find(record.Id)!;
        Assert.False(updated.Enabled);
        Assert.Equal(group.Id, updated.GroupId);
        Assert.Equal(5, updated.SortOrder);
    }

    [Fact]
    public void Delete_removes_the_record()
    {
        var record = _repository.Insert("My Dict", "stardict", "C:/dict.ifo", "abc123");

        var deleted = _repository.Delete(record.Id);

        Assert.True(deleted);
        Assert.Null(_repository.Find(record.Id));
    }
}
