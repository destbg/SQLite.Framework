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
    private static int SeedThenNextId(bool rebuild, bool deleteAll, MigrateMode mode)
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<AiSeqRow>();
        db.Table<AiSeqRow>().Add(new AiSeqRow { Name = "a" });
        db.Table<AiSeqRow>().Add(new AiSeqRow { Name = "b" });
        db.Table<AiSeqRow>().Add(new AiSeqRow { Name = "c" });
        db.Execute(deleteAll ? "DELETE FROM \"AiSeq\"" : "DELETE FROM \"AiSeq\" WHERE \"Id\" = 3");

        if (rebuild)
        {
            db.Schema.Table<AiSeqRow>().Migrate(mode, m => m.Set(x => x.Name, x => x.Name));
        }

        AiSeqRow inserted = new() { Name = "d" };
        db.Table<AiSeqRow>().Add(inserted);
        return inserted.Id;
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void RebuildPreservesHighWaterMark_AfterTopDelete(MigrateMode mode)
    {
        int oracle = SeedThenNextId(rebuild: false, deleteAll: false, mode);
        int actual = SeedThenNextId(rebuild: true, deleteAll: false, mode);

        Assert.Equal(4, oracle);
        Assert.Equal(oracle, actual);
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void RebuildPreservesHighWaterMark_AfterAllDeleted(MigrateMode mode)
    {
        int oracle = SeedThenNextId(rebuild: false, deleteAll: true, mode);
        int actual = SeedThenNextId(rebuild: true, deleteAll: true, mode);

        Assert.Equal(4, oracle);
        Assert.Equal(oracle, actual);
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void MigrateWithoutRebuildPreservesHighWaterMark(MigrateMode mode)
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<AiSeqRow>();
        db.Table<AiSeqRow>().Add(new AiSeqRow { Name = "a" });
        db.Table<AiSeqRow>().Add(new AiSeqRow { Name = "b" });
        db.Table<AiSeqRow>().Add(new AiSeqRow { Name = "c" });
        db.Execute("DELETE FROM \"AiSeq\" WHERE \"Id\" = 3");

        db.Schema.Table<AiSeqRow>().Migrate(mode);

        AiSeqRow inserted = new() { Name = "d" };
        db.Table<AiSeqRow>().Add(inserted);

        Assert.Equal(4, inserted.Id);
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void DriftRebuildPreservesHighWaterMark(MigrateMode mode)
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"AiSeq\" (\"Id\" INTEGER PRIMARY KEY AUTOINCREMENT, \"Name\" TEXT NOT NULL, \"Old\" TEXT)");
        db.Execute("INSERT INTO \"AiSeq\" (\"Name\") VALUES ('a'), ('b'), ('c')");
        db.Execute("DELETE FROM \"AiSeq\" WHERE \"Id\" = 3");

        db.Schema.Table<AiSeqRow>().Migrate(mode);

        AiSeqRow inserted = new() { Name = "d" };
        db.Table<AiSeqRow>().Add(inserted);

        Assert.Equal(4, inserted.Id);
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void AddingAutoIncrementViaMigratePreservesRowIds(MigrateMode mode)
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"AiSeq\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"AiSeq\" (\"Name\") VALUES ('a'), ('b'), ('c')");

        db.Schema.Table<AiSeqRow>().Migrate(mode);

        AiSeqRow inserted = new() { Name = "d" };
        db.Table<AiSeqRow>().Add(inserted);

        Assert.Equal(4, inserted.Id);
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void NonAutoIncrementTableRebuildPreservesRows(MigrateMode mode)
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"PlainSeq\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Old\" TEXT)");
        db.Execute("INSERT INTO \"PlainSeq\" (\"Id\", \"Name\") VALUES (1, 'a'), (2, 'b'), (5, 'c')");

        db.Schema.Table<PlainSeqRow>().Migrate(mode);

        List<int> expected = new List<int> { 1, 2, 5 };
        List<int> actual = db.Table<PlainSeqRow>().OrderBy(r => r.Id).Select(r => r.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void RebuildOfNeverInsertedAutoIncrementTableStartsFresh(MigrateMode mode)
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<AiSeqRow>();

        db.Schema.Table<AiSeqRow>().Migrate(mode, m => m.Set(x => x.Name, x => x.Name));

        AiSeqRow inserted = new() { Name = "a" };
        db.Table<AiSeqRow>().Add(inserted);

        Assert.Equal(1, inserted.Id);
    }
}
