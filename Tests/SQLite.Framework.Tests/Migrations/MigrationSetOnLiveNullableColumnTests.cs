using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("LiveNullableSetRows")]
public class LiveNullableSetRow
{
    [Key]
    public int Id { get; set; }

    public int Val { get; set; }

    public string? Tag { get; set; }
}

public class MigrationSetOnLiveNullableColumnTests
{
    [Fact]
    public void AConstantSetOnALiveNullableColumnAfterADataStepMatchesStepwise()
    {
        Assert.Equal(Stepwise(), Collapsed());
    }

    [Fact]
    public void AConstantSetOnALiveNullableColumnKeepsTheExpectedValues()
    {
        Assert.Equal([(10, "set"), (5, "set")], Stepwise());
    }

    private static List<(int Val, string? Tag)> Stepwise()
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

    private static List<(int Val, string? Tag)> Collapsed()
    {
        using TestDatabase db = new(useFile: true);
        Seed(db);
        SQLiteMigrationRunner runner = db.Schema.Migrations();
        Chain(runner, 3);
        runner.Migrate();
        return Rows(db);
    }

    private static void Chain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.Sql(
            "INSERT INTO \"LiveNullableSetRows\" (\"Id\", \"Val\", \"Tag\") VALUES (5, 5, 'old')"));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.TableChanged<LiveNullableSetRow>(s => s.Set(x => x.Tag, "set")));
        }
    }

    private static void Seed(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"LiveNullableSetRows\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" INTEGER NOT NULL, \"Tag\" TEXT)");
        db.Execute("INSERT INTO \"LiveNullableSetRows\" (\"Id\", \"Val\", \"Tag\") VALUES (1, 10, 'old')");
        db.Pragmas.UserVersion = 1;
    }

    private static List<(int Val, string? Tag)> Rows(TestDatabase db)
    {
        return db.Table<LiveNullableSetRow>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Val, x.Tag })
            .ToList()
            .Select(x => (x.Val, x.Tag))
            .ToList();
    }
}
