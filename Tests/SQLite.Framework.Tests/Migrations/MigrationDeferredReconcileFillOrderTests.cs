using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H20Mig_FillOrder")]
public class H20MigFillOrderRow
{
    [Key]
    public int Id { get; set; }

    public int Val { get; set; }

    public string? Tag { get; set; }
}

public class MigrationDeferredReconcileFillOrderTests
{
    private static void Seed(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"H20Mig_FillOrder\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" INTEGER, \"Tag\" TEXT)");
        db.Execute("INSERT INTO \"H20Mig_FillOrder\" (\"Id\", \"Val\", \"Tag\") VALUES (1, 10, 'old')");
        db.Pragmas.UserVersion = 1;
    }

    [Fact]
    public void EarlierVersionFillDoesNotTouchLaterInsertedRows()
    {
        using TestDatabase stepwise = new(useFile: true);
        Seed(stepwise);
        stepwise.Schema.Migrations()
            .Version(2, m => m.TableChanged<H20MigFillOrderRow>(s => s.Set(x => x.Tag, "v2")))
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(2, m => m.TableChanged<H20MigFillOrderRow>(s => s.Set(x => x.Tag, "v2")))
            .Version(3, m => m.Insert(new H20MigFillOrderRow { Id = 2, Val = 50, Tag = "ins" }))
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(2, m => m.TableChanged<H20MigFillOrderRow>(s => s.Set(x => x.Tag, "v2")))
            .Version(3, m => m.Insert(new H20MigFillOrderRow { Id = 2, Val = 50, Tag = "ins" }))
            .Version(4, m => m.TableChanged<H20MigFillOrderRow>(s => s.Set(x => x.Val, r => r.Val + 1)))
            .Migrate();

        using TestDatabase collapsed = new(useFile: true);
        Seed(collapsed);
        collapsed.Schema.Migrations()
            .Version(2, m => m.TableChanged<H20MigFillOrderRow>(s => s.Set(x => x.Tag, "v2")))
            .Version(3, m => m.Insert(new H20MigFillOrderRow { Id = 2, Val = 50, Tag = "ins" }))
            .Version(4, m => m.TableChanged<H20MigFillOrderRow>(s => s.Set(x => x.Val, r => r.Val + 1)))
            .Migrate();

        List<(int Val, string? Tag)> stepwiseRows = stepwise.Table<H20MigFillOrderRow>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Val, x.Tag })
            .ToList()
            .Select(x => (x.Val, x.Tag))
            .ToList();
        List<(int Val, string? Tag)> collapsedRows = collapsed.Table<H20MigFillOrderRow>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Val, x.Tag })
            .ToList()
            .Select(x => (x.Val, x.Tag))
            .ToList();

        Assert.Equal([(11, "v2"), (51, "ins")], stepwiseRows);
        Assert.Equal(stepwiseRows, collapsedRows);
    }
}
