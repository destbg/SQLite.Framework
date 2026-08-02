using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SecGDropFillRows")]
public class SecGDropFillRow
{
    [Key]
    public int Id { get; set; }

    public string? Note { get; set; }
}

public class MigrationDroppedTableFillTests
{
    [Fact]
    public void AFillReadingAColumnOfADroppedTableIsSkippedLikeStepwise()
    {
        using TestDatabase stepwise = new(useFile: true);
        Seed(stepwise);
        Chain(stepwise.Schema.Migrations(), 2).Migrate();
        Chain(stepwise.Schema.Migrations(), 3).Migrate();

        using TestDatabase collapsed = new(useFile: true);
        Seed(collapsed);
        Chain(collapsed.Schema.Migrations(), 3).Migrate();

        List<string?> stepwiseNotes = Notes(stepwise);
        List<string?> collapsedNotes = Notes(collapsed);

        Assert.Empty(stepwiseNotes);
        Assert.Equal(stepwiseNotes, collapsedNotes);
    }

    private static void Seed(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"SecGDropFillRows\" (\"Id\" INTEGER PRIMARY KEY, \"Note\" TEXT, \"Legacy\" TEXT)");
        db.Execute("INSERT INTO \"SecGDropFillRows\" (\"Id\", \"Note\", \"Legacy\") VALUES (1, NULL, 'a'), (2, NULL, 'b')");
        db.Pragmas.UserVersion = 1;
    }

    private static SQLiteMigrationRunner Chain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.DropTable<SecGDropFillRow>());
        if (upTo >= 3)
        {
            runner.Version(3, m => m
                .CreateTable<SecGDropFillRow>()
                .TableChanged<SecGDropFillRow>(s => s.Set(x => x.Note, r => SQLiteColumn.Of<string?>(r, "Legacy"))));
        }

        return runner;
    }

    private static List<string?> Notes(TestDatabase db)
    {
        return db.Table<SecGDropFillRow>()
            .OrderBy(x => x.Id)
            .Select(x => x.Note)
            .ToList();
    }
}
