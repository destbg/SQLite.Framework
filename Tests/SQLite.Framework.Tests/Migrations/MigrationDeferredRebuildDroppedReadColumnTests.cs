using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21aDropRead")]
public class H21aDropReadRow
{
    [Key]
    public int Id { get; set; }

    public int Val { get; set; }

    public string? Note { get; set; }
}

public class MigrationDeferredRebuildDroppedReadColumnTests
{
    [Fact]
    public void CopyFromALegacyColumnSurvivesALaterColumnDrop()
    {
        using TestDatabase stepwise = new(useFile: true);
        Seed(stepwise);
        Chain(stepwise.Schema.Migrations(), 2).Migrate();
        Chain(stepwise.Schema.Migrations(), 3).Migrate();
        Chain(stepwise.Schema.Migrations(), 4).Migrate();

        using TestDatabase collapsed = new(useFile: true);
        Seed(collapsed);
        Chain(collapsed.Schema.Migrations(), 4).Migrate();

        List<(int Val, string? Note)> stepwiseRows = Rows(stepwise);
        List<(int Val, string? Note)> collapsedRows = Rows(collapsed);

        Assert.Equal([(11, "keepme")], stepwiseRows);
        Assert.Equal(stepwiseRows, collapsedRows);
    }

    private static void Seed(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"H21aDropRead\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" INTEGER, \"Legacy\" TEXT)");
        db.Execute("INSERT INTO \"H21aDropRead\" (\"Id\", \"Val\", \"Legacy\") VALUES (1, 10, 'keepme')");
        db.Pragmas.UserVersion = 1;
    }

    private static SQLiteMigrationRunner Chain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.TableChanged<H21aDropReadRow>(
            s => s.Set(x => x.Note, r => SQLiteColumn.Of<string?>(r, "Legacy"))));
        if (upTo >= 3)
        {
            runner.Version(3, m => m
                .DropColumn<H21aDropReadRow>("Legacy")
                .Delete<H21aDropReadRow>(x => x.Id == 999));
        }

        if (upTo >= 4)
        {
            runner.Version(4, m => m.TableChanged<H21aDropReadRow>(s => s.Set(x => x.Val, r => r.Val + 1)));
        }

        return runner;
    }

    private static List<(int Val, string? Note)> Rows(TestDatabase db)
    {
        return db.Table<H21aDropReadRow>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Val, x.Note })
            .ToList()
            .Select(x => (x.Val, x.Note))
            .ToList();
    }
}
