using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22lOwnShift")]
public class H22lOwnShiftRow
{
    [Key]
    public int Id { get; set; }

    [Indexed(IsUnique = true)]
    public int Position { get; set; }
}

public class MigrationUniqueColumnShiftFillTests
{
    [Fact]
    public void ShiftingEveryUniqueValueMatchesTheStepwiseRun()
    {
        Assert.Equal(Stepwise(), Collapsed());
    }

    [Fact]
    public void ShiftingEveryUniqueValueKeepsEveryRowValue()
    {
        List<(int Id, int Position)> source = [(1, 1), (2, 2)];
        List<(int Id, int Position)> expected = source
            .OrderBy(r => r.Id)
            .Select(r => (Id: r.Id, Position: r.Position + 1))
            .ToList();

        Assert.Equal(expected, Collapsed());
    }

    private static void Chain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.Sql("UPDATE \"H22lOwnShift\" SET \"Id\" = \"Id\""));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.TableChanged<H22lOwnShiftRow>(s => s.Set(x => x.Position, p => p.Position + 1)));
        }
    }

    private static List<(int Id, int Position)> Stepwise()
    {
        using TestDatabase db = new(useFile: true);
        Seed(db);
        for (int upTo = 2; upTo <= 3; upTo++)
        {
            SQLiteMigrationRunner runner = db.Schema.Migrations();
            Chain(runner, upTo);
            runner.Migrate();
        }

        return Rows(db);
    }

    private static List<(int Id, int Position)> Collapsed()
    {
        using TestDatabase db = new(useFile: true);
        Seed(db);
        SQLiteMigrationRunner runner = db.Schema.Migrations();
        Chain(runner, 3);
        runner.Migrate();
        return Rows(db);
    }

    private static void Seed(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"H22lOwnShift\" (\"Id\" INTEGER PRIMARY KEY, \"Position\" INTEGER)");
        db.Execute("INSERT INTO \"H22lOwnShift\" (\"Id\", \"Position\") VALUES (1, 1)");
        db.Execute("INSERT INTO \"H22lOwnShift\" (\"Id\", \"Position\") VALUES (2, 2)");
        db.Pragmas.UserVersion = 1;
    }

    private static List<(int Id, int Position)> Rows(TestDatabase db)
    {
        return db.Table<H22lOwnShiftRow>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.Position })
            .ToList()
            .Select(x => (x.Id, x.Position))
            .ToList();
    }
}
