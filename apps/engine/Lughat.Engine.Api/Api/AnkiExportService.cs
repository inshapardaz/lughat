using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Lughat.Engine.Api.Api;

public sealed record AnkiCard(string Front, string Back);

/// <summary>
/// Builds a minimal, valid Anki .apkg package (spec §6) — a zip containing a SQLite
/// "collection.anki2" using Anki's legacy schema (one deck, one Basic note type: Front/Back)
/// plus a media manifest. Embedded media isn't bundled yet — same documented gap as
/// MediaEndpoints.cs (bearer-token auth doesn't reach plain media requests, so there's
/// nothing reliable to bundle from yet); every card here is HTML text only.
/// </summary>
public static class AnkiExportService
{
    private const long ModelId = 1;
    private const long DeckId = 2;

    public static byte[] BuildPackage(string deckName, IReadOnlyList<AnkiCard> cards)
    {
        if (cards.Count == 0)
        {
            throw new ArgumentException("At least one card is required.", nameof(cards));
        }

        var dbPath = Path.Combine(Path.GetTempPath(), $"lughat-anki-{Guid.NewGuid():n}.anki2");
        try
        {
            BuildCollectionDatabase(dbPath, deckName, cards);

            using var zipStream = new MemoryStream();
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var dbEntry = archive.CreateEntry("collection.anki2", CompressionLevel.Optimal);
                using (var entryStream = dbEntry.Open())
                using (var fileStream = File.OpenRead(dbPath))
                {
                    fileStream.CopyTo(entryStream);
                }

                var mediaEntry = archive.CreateEntry("media", CompressionLevel.Optimal);
                using (var entryStream = mediaEntry.Open())
                using (var writer = new StreamWriter(entryStream))
                {
                    writer.Write("{}"); // no bundled media yet — see the class doc comment
                }
            }

            return zipStream.ToArray();
        }
        finally
        {
            File.Delete(dbPath);
        }
    }

    private static void BuildCollectionDatabase(string dbPath, string deckName, IReadOnlyList<AnkiCard> cards)
    {
        // Pooling off — same reasoning as AppDatabase used to have: a pooled connection keeps
        // a native handle open past Dispose(), which stops this throwaway file from being
        // deleted right after BuildPackage() reads it back into the zip.
        using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=False");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE col (id integer primary key, crt integer not null, mod integer not null, scm integer not null, ver integer not null, dty integer not null, usn integer not null, ls integer not null, conf text not null, models text not null, decks text not null, dconf text not null, tags text not null);
            CREATE TABLE notes (id integer primary key, guid text not null, mid integer not null, mod integer not null, usn integer not null, tags text not null, flds text not null, sfld text not null, csum integer not null, flags integer not null, data text not null);
            CREATE TABLE cards (id integer primary key, nid integer not null, did integer not null, ord integer not null, mod integer not null, usn integer not null, type integer not null, queue integer not null, due integer not null, ivl integer not null, factor integer not null, reps integer not null, lapses integer not null, left integer not null, odue integer not null, odid integer not null, flags integer not null, data text not null);
            CREATE TABLE revlog (id integer primary key, cid integer not null, usn integer not null, ease integer not null, ivl integer not null, lastIvl integer not null, factor integer not null, time integer not null, type integer not null);
            CREATE TABLE graves (usn integer not null, oid integer not null, type integer not null);
            CREATE INDEX ix_notes_usn on notes (usn);
            CREATE INDEX ix_cards_usn on cards (usn);
            CREATE INDEX ix_revlog_usn on revlog (usn);
            CREATE INDEX ix_cards_nid on cards (nid);
            CREATE INDEX ix_cards_sched on cards (did, queue, due);
            CREATE INDEX ix_notes_csum on notes (csum);
            """;
        command.ExecuteNonQuery();

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        InsertCollectionRow(connection, deckName, now, nowMs);

        var noteId = nowMs;
        var cardId = nowMs;
        foreach (var card in cards)
        {
            InsertNoteAndCard(connection, noteId, cardId, now, card);
            noteId++;
            cardId++;
        }
    }

    private static void InsertCollectionRow(SqliteConnection connection, string deckName, long now, long nowMs)
    {
        var models = new Dictionary<string, object>
        {
            [ModelId.ToString()] = new
            {
                id = ModelId,
                name = "Lughat Basic",
                type = 0,
                mod = now,
                usn = 0,
                sortf = 0,
                did = DeckId,
                tmpls = new object[]
                {
                    new
                    {
                        name = "Card 1",
                        ord = 0,
                        qfmt = "{{Front}}",
                        afmt = "{{FrontSide}}<hr id=\"answer\">{{Back}}",
                        did = (object?)null,
                        bqfmt = "",
                        bafmt = "",
                    },
                },
                flds = new object[]
                {
                    new { name = "Front", ord = 0, sticky = false, rtl = false, font = "Arial", size = 20 },
                    new { name = "Back", ord = 1, sticky = false, rtl = false, font = "Arial", size = 20 },
                },
                css = ".card { font-family: arial; font-size: 20px; text-align: center; }",
                latexPre = "",
                latexPost = "",
                req = new object[] { new object[] { 0, "any", new[] { 0 } } },
            },
        };

        var decks = new Dictionary<string, object>
        {
            [DeckId.ToString()] = DeckJson(DeckId, deckName, now),
            ["1"] = DeckJson(1, "Default", now),
        };

        var dconf = new Dictionary<string, object>
        {
            ["1"] = new
            {
                id = 1,
                name = "Default",
                mod = now,
                usn = 0,
                maxTaken = 60,
                autoplay = true,
                timer = 0,
                replayq = true,
                @new = new { perDay = 20, delays = new[] { 1, 10 }, initialFactor = 2500, ints = new[] { 1, 4, 7 }, order = 1, bury = false },
                rev = new { perDay = 200, ease4 = 1.3, fuzz = 0.05, minSpace = 1, ivlFct = 1, maxIvl = 36500, bury = false },
                lapse = new { delays = new[] { 10 }, mult = 0, minInt = 1, leechFails = 8, leechAction = 0 },
                misc = new { },
            },
        };

        var conf = new
        {
            curDeck = DeckId,
            curModel = ModelId.ToString(),
            nextPos = 1,
            sortType = "noteFld",
            sortBackwards = false,
            addToCur = true,
            estTimes = true,
            dueCounts = true,
        };

        var insertCol = connection.CreateCommand();
        insertCol.CommandText =
            "INSERT INTO col (id, crt, mod, scm, ver, dty, usn, ls, conf, models, decks, dconf, tags) " +
            "VALUES (1, @crt, @mod, @scm, 11, 0, 0, 0, @conf, @models, @decks, @dconf, '{}')";
        insertCol.Parameters.AddWithValue("@crt", now);
        insertCol.Parameters.AddWithValue("@mod", nowMs);
        insertCol.Parameters.AddWithValue("@scm", nowMs);
        insertCol.Parameters.AddWithValue("@conf", JsonSerializer.Serialize(conf));
        insertCol.Parameters.AddWithValue("@models", JsonSerializer.Serialize(models));
        insertCol.Parameters.AddWithValue("@decks", JsonSerializer.Serialize(decks));
        insertCol.Parameters.AddWithValue("@dconf", JsonSerializer.Serialize(dconf));
        insertCol.ExecuteNonQuery();
    }

    private static object DeckJson(long id, string name, long now) => new
    {
        id,
        name,
        mod = now,
        usn = 0,
        collapsed = false,
        extendNew = 0,
        extendRev = 0,
        conf = 1,
        desc = "",
        dyn = 0,
    };

    private static void InsertNoteAndCard(SqliteConnection connection, long noteId, long cardId, long now, AnkiCard card)
    {
        // The literal character between Front and Back below is U+001F (unit separator) —
        // Anki's required field delimiter in notes.flds. It doesn't render visibly in an
        // editor/diff, so don't "clean up" what looks like Front and Back run together.
        var fields = $"{card.Front}{card.Back}";

        var insertNote = connection.CreateCommand();
        insertNote.CommandText =
            "INSERT INTO notes (id, guid, mid, mod, usn, tags, flds, sfld, csum, flags, data) " +
            "VALUES (@id, @guid, @mid, @mod, -1, '', @flds, @sfld, @csum, 0, '')";
        insertNote.Parameters.AddWithValue("@id", noteId);
        insertNote.Parameters.AddWithValue("@guid", Guid.NewGuid().ToString("n"));
        insertNote.Parameters.AddWithValue("@mid", ModelId);
        insertNote.Parameters.AddWithValue("@mod", now);
        insertNote.Parameters.AddWithValue("@flds", fields);
        insertNote.Parameters.AddWithValue("@sfld", card.Front);
        insertNote.Parameters.AddWithValue("@csum", FieldChecksum(card.Front));
        insertNote.ExecuteNonQuery();

        var insertCard = connection.CreateCommand();
        insertCard.CommandText =
            "INSERT INTO cards (id, nid, did, ord, mod, usn, type, queue, due, ivl, factor, reps, lapses, left, odue, odid, flags, data) " +
            "VALUES (@id, @nid, @did, 0, @mod, -1, 0, 0, @due, 0, 0, 0, 0, 0, 0, 0, 0, '')";
        insertCard.Parameters.AddWithValue("@id", cardId);
        insertCard.Parameters.AddWithValue("@nid", noteId);
        insertCard.Parameters.AddWithValue("@did", DeckId);
        insertCard.Parameters.AddWithValue("@mod", now);
        insertCard.Parameters.AddWithValue("@due", cardId % 1_000_000_000); // stable relative ordering, not wall-clock
        insertCard.ExecuteNonQuery();
    }

    /// <summary>
    /// Anki's own "first 8 hex digits of sha1(field)" convention for the notes.csum column,
    /// used for duplicate detection — not a security checksum.
    /// </summary>
    private static long FieldChecksum(string field)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(field));
        return Convert.ToInt64(Convert.ToHexString(hash)[..8], 16);
    }
}
