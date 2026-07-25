using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("FillOnceCounter")]
public class FillOnceCounterRow
{
    [Key]
    public int Id { get; set; }

    public int Hits { get; set; }
}

public class MigrationNotNullFillSingleApplicationTests
{
    [Fact]
    public void SelfReferentialFillReadsTheOldRowOnce()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"FillOnceCounter\" (\"Id\" INTEGER PRIMARY KEY, \"Hits\" INTEGER)");
        db.Execute("INSERT INTO \"FillOnceCounter\" (\"Id\", \"Hits\") VALUES (1, 10)");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<FillOnceCounterRow>(s => s.Set(r => r.Hits, r => r.Hits + 1)))
            .Migrate();

        Assert.Equal(11, db.Table<FillOnceCounterRow>().Single().Hits);
    }

    [Fact]
    public void SelfReferentialFillReadsTheOldRowOnceWhenRebuildIsForced()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"FillOnceCounter\" (\"Id\" INTEGER PRIMARY KEY, \"Hits\" INTEGER)");
        db.Execute("INSERT INTO \"FillOnceCounter\" (\"Id\", \"Hits\") VALUES (1, 20)");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<FillOnceCounterRow>(s => s.Set(r => r.Hits, r => r.Hits + 1), rebuild: true))
            .Migrate();

        Assert.Equal(21, db.Table<FillOnceCounterRow>().Single().Hits);
    }
}
