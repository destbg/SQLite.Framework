using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("AiSeq")]
file sealed class AiSeqRow
{
    [Key, AutoIncrement]
    public int Id { get; set; }

    public required string Name { get; set; }
}

[Table("PlainSeq")]
file sealed class PlainSeqRow
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }
}

public class MigrateAutoIncrementTests
{
    private static int SeedThenNextId(bool rebuild, bool deleteAll)
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<AiSeqRow>();
        db.Table<AiSeqRow>().Add(new AiSeqRow { Name = "a" });
        db.Table<AiSeqRow>().Add(new AiSeqRow { Name = "b" });
        db.Table<AiSeqRow>().Add(new AiSeqRow { Name = "c" });
        db.Execute(deleteAll ? "DELETE FROM \"AiSeq\"" : "DELETE FROM \"AiSeq\" WHERE \"Id\" = 3");

        if (rebuild)
        {
            db.Schema.Table<AiSeqRow>().Migrate(m => m.Set(x => x.Name, x => x.Name));
        }

        AiSeqRow inserted = new() { Name = "d" };
        db.Table<AiSeqRow>().Add(inserted);
        return inserted.Id;
    }

    [Fact]
    public void RebuildPreservesHighWaterMark_AfterTopDelete()
    {
        int oracle = SeedThenNextId(rebuild: false, deleteAll: false);
        int actual = SeedThenNextId(rebuild: true, deleteAll: false);

        Assert.Equal(4, oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void RebuildPreservesHighWaterMark_AfterAllDeleted()
    {
        int oracle = SeedThenNextId(rebuild: false, deleteAll: true);
        int actual = SeedThenNextId(rebuild: true, deleteAll: true);

        Assert.Equal(4, oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void MigrateWithoutRebuildPreservesHighWaterMark()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<AiSeqRow>();
        db.Table<AiSeqRow>().Add(new AiSeqRow { Name = "a" });
        db.Table<AiSeqRow>().Add(new AiSeqRow { Name = "b" });
        db.Table<AiSeqRow>().Add(new AiSeqRow { Name = "c" });
        db.Execute("DELETE FROM \"AiSeq\" WHERE \"Id\" = 3");

        db.Schema.Table<AiSeqRow>().Migrate();

        AiSeqRow inserted = new() { Name = "d" };
        db.Table<AiSeqRow>().Add(inserted);

        Assert.Equal(4, inserted.Id);
    }

    [Fact]
    public void DriftRebuildPreservesHighWaterMark()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"AiSeq\" (\"Id\" INTEGER PRIMARY KEY AUTOINCREMENT, \"Name\" TEXT NOT NULL, \"Old\" TEXT)");
        db.Execute("INSERT INTO \"AiSeq\" (\"Name\") VALUES ('a'), ('b'), ('c')");
        db.Execute("DELETE FROM \"AiSeq\" WHERE \"Id\" = 3");

        db.Schema.Table<AiSeqRow>().Migrate();

        AiSeqRow inserted = new() { Name = "d" };
        db.Table<AiSeqRow>().Add(inserted);

        Assert.Equal(4, inserted.Id);
    }

    [Fact]
    public void AddingAutoIncrementViaMigratePreservesRowIds()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"AiSeq\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"AiSeq\" (\"Name\") VALUES ('a'), ('b'), ('c')");

        db.Schema.Table<AiSeqRow>().Migrate();

        AiSeqRow inserted = new() { Name = "d" };
        db.Table<AiSeqRow>().Add(inserted);

        Assert.Equal(4, inserted.Id);
    }

    [Fact]
    public void NonAutoIncrementTableRebuildPreservesRows()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"PlainSeq\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Old\" TEXT)");
        db.Execute("INSERT INTO \"PlainSeq\" (\"Id\", \"Name\") VALUES (1, 'a'), (2, 'b'), (5, 'c')");

        db.Schema.Table<PlainSeqRow>().Migrate();

        List<int> expected = new List<int> { 1, 2, 5 };
        List<int> actual = db.Table<PlainSeqRow>().OrderBy(r => r.Id).Select(r => r.Id).ToList();

        Assert.Equal(expected, actual);
    }
}
