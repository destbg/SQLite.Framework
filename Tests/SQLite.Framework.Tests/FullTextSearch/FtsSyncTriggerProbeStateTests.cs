using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Models;

namespace SQLite.Framework.Tests;

[Table("ProbeStateNote")]
public class ProbeStateNote
{
    [Key]
    public int Id { get; set; }

    public required string Body { get; set; }
}

[FullTextSearch(ContentMode = FtsContentMode.External, ContentTable = typeof(ProbeStateNote), AutoSync = FtsAutoSync.Triggers)]
[Table("ProbeStateNoteSearch")]
public class ProbeStateNoteSearch
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public required string Body { get; set; }
}

public class FtsSyncTriggerProbeStateTests
{
    [Fact]
    public void AWriterThatDidNotDeclareTheIndexRecordsTheTriggerProbe()
    {
        string path = $"ProbeState_{Guid.NewGuid():N}.db3";
        try
        {
            using (SQLiteDatabase setup = Open(path))
            {
                setup.Table<ProbeStateNote>().Schema.CreateTable();
                setup.Table<ProbeStateNoteSearch>().Schema.CreateTable();
            }

            using SQLiteDatabase writer = Open(path);
            TableMapping mapping = writer.TableMapping(typeof(ProbeStateNote));
            Assert.False(mapping.FtsSyncTriggersProbed);

            writer.Table<ProbeStateNote>().AddOrUpdate(new ProbeStateNote { Id = 1, Body = "apple" });

            Assert.True(mapping.FtsSyncTriggersProbed);
        }
        finally
        {
            DeleteFile(path);
        }
    }

    private static SQLiteDatabase Open(string path)
    {
        SQLiteOptionsBuilder builder = new(path);
#if SQLITECIPHER
        builder.UseEncryptionKey("test-key");
#endif
        return new SQLiteDatabase(builder.Build());
    }

    private static void DeleteFile(string path)
    {
        foreach (string candidate in new[] { path, path + "-wal", path + "-shm" })
        {
            if (File.Exists(candidate))
            {
                File.Delete(candidate);
            }
        }
    }
}
