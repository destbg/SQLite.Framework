using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ChainedFillScore")]
public class ChainedFillScoreRow
{
    [Key]
    public int Id { get; set; }

    public int Score { get; set; }
}

[Table("LaterFillBook")]
public class LaterFillBookRow
{
    [Key]
    public int Id { get; set; }

    public string Slug { get; set; } = "";
}

[Table("OverrideFillScore")]
public class OverrideFillScoreRow
{
    [Key]
    public int Id { get; set; }

    public int Score { get; set; }
}

public class MigrationSchemaPhaseFillTests
{
    [Fact]
    public void ChainedConstantSetOnNewNotNullColumnKeepsLastValue()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"ChainedFillScore\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Execute("INSERT INTO \"ChainedFillScore\" (\"Id\") VALUES (1)");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<ChainedFillScoreRow>(s => s.Set(r => r.Score, 5).Set(r => r.Score, 7)))
            .Migrate();

        Assert.Equal(7, db.Table<ChainedFillScoreRow>().Single().Score);
    }

    [Fact]
    public void LaterVersionOutOfModelResetOfSchemaPhaseColumnApplies()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"LaterFillBook\" (\"Id\" INTEGER PRIMARY KEY, \"Legacy\" TEXT)");
        db.Execute("INSERT INTO \"LaterFillBook\" (\"Id\", \"Legacy\") VALUES (1, 'old')");

        db.Schema.Migrations()
            .Version(2, m => m.TableChanged<LaterFillBookRow>(s => s.Set(b => b.Slug, "temp")))
            .Version(3, m => m.TableChanged<LaterFillBookRow>(s => s.Set(b => b.Slug, b => SQLiteColumn.Of<string>(b, "Legacy"))))
            .Migrate();

        Assert.Equal("old", db.Table<LaterFillBookRow>().Single().Slug);
    }

    [Fact]
    public void LaterConstantSetOverridesAnEarlierOutOfModelSchemaPhaseFill()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"OverrideFillScore\" (\"Id\" INTEGER PRIMARY KEY, \"Score\" INTEGER NOT NULL DEFAULT 0, \"Legacy\" INTEGER)");
        db.Execute("INSERT INTO \"OverrideFillScore\" (\"Id\", \"Score\", \"Legacy\") VALUES (1, 0, 42)");

        db.Schema.Migrations()
            .Version(2, m => m.TableChanged<OverrideFillScoreRow>(s => s.Set(r => r.Score, r => SQLiteColumn.Of<int>(r, "Legacy"))))
            .Version(3, m => m.TableChanged<OverrideFillScoreRow>(s => s.Set(r => r.Score, 99)))
            .Migrate();

        Assert.Equal(99, db.Table<OverrideFillScoreRow>().Single().Score);
    }

    [Fact]
    public void ChainedFillsAcrossVersionsOnANewColumnMatchStepwise()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"OverrideFillScore\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Execute("INSERT INTO \"OverrideFillScore\" (\"Id\") VALUES (1)");

        db.Schema.Migrations()
            .Version(2, m => m.TableChanged<OverrideFillScoreRow>(s => s.Set(r => r.Score, 10)))
            .Version(3, m => m.TableChanged<OverrideFillScoreRow>(s => s.Set(r => r.Score, r => r.Score + 5)))
            .Migrate();

        Assert.Equal(15, db.Table<OverrideFillScoreRow>().Single().Score);
    }
}
