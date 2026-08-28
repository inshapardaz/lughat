using System.IO.Compression;
using Lughat.Engine.Api.Api;
using Microsoft.Data.Sqlite;

namespace Lughat.Engine.Tests.Api;

public class AnkiExportServiceTests
{
    [Fact]
    public void BuildPackage_produces_a_zip_with_a_valid_anki_collection_and_media_manifest()
    {
        var cards = new[]
        {
            new AnkiCard("apple", "A round fruit."),
            new AnkiCard("book", "A set of printed pages."),
        };

        var packageBytes = AnkiExportService.BuildPackage("Lughat Export", cards);

        using var zipStream = new MemoryStream(packageBytes);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        var collectionEntry = archive.GetEntry("collection.anki2");
        Assert.NotNull(collectionEntry);

        var mediaEntry = archive.GetEntry("media");
        Assert.NotNull(mediaEntry);
        using (var reader = new StreamReader(mediaEntry!.Open()))
        {
            Assert.Equal("{}", reader.ReadToEnd());
        }

        var dbPath = Path.Combine(Path.GetTempPath(), $"anki-test-{Guid.NewGuid():n}.anki2");
        try
        {
            using (var entryStream = collectionEntry!.Open())
            using (var fileStream = File.Create(dbPath))
            {
                entryStream.CopyTo(fileStream);
            }

            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            AssertScalar(connection, "SELECT COUNT(*) FROM col", 1L);
            AssertScalar(connection, "SELECT COUNT(*) FROM notes", 2L);
            AssertScalar(connection, "SELECT COUNT(*) FROM cards", 2L);

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT flds, sfld FROM notes ORDER BY id LIMIT 1";
            using var reader = command.ExecuteReader();
            Assert.True(reader.Read());

            var fields = reader.GetString(0);
            var parts = fields.Split('');
            Assert.Equal(2, parts.Length);
            Assert.Equal("apple", parts[0]);
            Assert.Equal("A round fruit.", parts[1]);
            Assert.Equal("apple", reader.GetString(1));

            using var deckNameCommand = connection.CreateCommand();
            deckNameCommand.CommandText = "SELECT decks FROM col";
            var decksJson = (string)deckNameCommand.ExecuteScalar()!;
            Assert.Contains("Lughat Export", decksJson);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(dbPath);
        }
    }

    [Fact]
    public void BuildPackage_throws_for_an_empty_card_list()
    {
        Assert.Throws<ArgumentException>(() => AnkiExportService.BuildPackage("Empty", []));
    }

    private static void AssertScalar(SqliteConnection connection, string sql, long expected)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        Assert.Equal(expected, (long)command.ExecuteScalar()!);
    }
}
