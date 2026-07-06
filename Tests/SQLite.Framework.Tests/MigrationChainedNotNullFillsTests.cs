using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ChainedFillCounter")]
public class ChainedFillCounterRow
{
    [Key]
    public int Id { get; set; }

    public int Hits { get; set; }
}

[FullTextSearch]
[Table("FillFtsNotes")]
public class FillFtsNoteRow
{
    [FullTextIndexed]
    public string Body { get; set; } = "";
}

[RTreeIndex]
[Table("FillRegions")]
public class FillRegionRow
{
    [Key]
    public int Id { get; set; }

    [RTreeMin("X")]
    public float MinX { get; set; }

    [RTreeMax("X")]
    public float MaxX { get; set; }
}

public class MigrationChainedNotNullFillsTests
{
    [Fact]
    public void ChainedFillsOnANullableColumnMatchStepwiseRuns()
    {
        using TestDatabase stepwise = new(useFile: true);
        stepwise.Execute("CREATE TABLE \"ChainedFillCounter\" (\"Id\" INTEGER PRIMARY KEY, \"Hits\" INTEGER)");
        stepwise.Execute("INSERT INTO \"ChainedFillCounter\" (\"Id\", \"Hits\") VALUES (1, 10)");
        stepwise.Schema.Migrations()
            .Version(1, m => m.TableChanged<ChainedFillCounterRow>(s => s.Set(r => r.Hits, r => r.Hits + 1)))
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(1, m => m.TableChanged<ChainedFillCounterRow>(s => s.Set(r => r.Hits, r => r.Hits + 1)))
            .Version(2, m => m.TableChanged<ChainedFillCounterRow>(s => s.Set(r => r.Hits, r => r.Hits * 2)))
            .Migrate();

        using TestDatabase collapsed = new(useFile: true);
        collapsed.Execute("CREATE TABLE \"ChainedFillCounter\" (\"Id\" INTEGER PRIMARY KEY, \"Hits\" INTEGER)");
        collapsed.Execute("INSERT INTO \"ChainedFillCounter\" (\"Id\", \"Hits\") VALUES (1, 10)");
        collapsed.Schema.Migrations()
            .Version(1, m => m.TableChanged<ChainedFillCounterRow>(s => s.Set(r => r.Hits, r => r.Hits + 1)))
            .Version(2, m => m.TableChanged<ChainedFillCounterRow>(s => s.Set(r => r.Hits, r => r.Hits * 2)))
            .Migrate();

        Assert.Equal(22, stepwise.Table<ChainedFillCounterRow>().Single().Hits);
        Assert.Equal(22, collapsed.Table<ChainedFillCounterRow>().Single().Hits);
    }

    [Fact]
    public void DuplicateFillsInOneVersionKeepTheLastValue()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"ChainedFillCounter\" (\"Id\" INTEGER PRIMARY KEY, \"Hits\" INTEGER NOT NULL)");
        db.Execute("INSERT INTO \"ChainedFillCounter\" (\"Id\", \"Hits\") VALUES (1, 10)");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<ChainedFillCounterRow>(s => s.Set(r => r.Hits, 5).Set(r => r.Hits, 7)))
            .Migrate();

        Assert.Equal(7, db.Table<ChainedFillCounterRow>().Single().Hits);
    }

    [Fact]
    public void FillOnAnExistingFullTextSearchTableStillRuns()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<FillFtsNoteRow>())
            .Migrate();
        db.Execute("INSERT INTO \"FillFtsNotes\" (\"Body\") VALUES ('old text')");

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<FillFtsNoteRow>())
            .Version(2, m => m.TableChanged<FillFtsNoteRow>(s => s.Set(r => r.Body, "new text")))
            .Migrate();

        Assert.Equal("new text", db.ExecuteScalar<string>("SELECT \"Body\" FROM \"FillFtsNotes\""));
    }

    [Fact]
    public void FillOnAnExistingRTreeTableStillRuns()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<FillRegionRow>())
            .Migrate();
        db.Execute("INSERT INTO \"FillRegions\" (\"Id\", \"MinX\", \"MaxX\") VALUES (1, 0, 10)");

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<FillRegionRow>())
            .Version(2, m => m.TableChanged<FillRegionRow>(s => s.Set(r => r.MinX, 5f)))
            .Migrate();

        Assert.Equal(5f, db.Table<FillRegionRow>().Single().MinX);
    }
}
