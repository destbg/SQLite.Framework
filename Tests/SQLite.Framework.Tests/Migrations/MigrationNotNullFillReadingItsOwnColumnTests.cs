using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22lOwnNullFill")]
public class H22lOwnNullFillRow
{
    [Key]
    public int Id { get; set; }

    public int Val { get; set; }
}

public class MigrationNotNullFillReadingItsOwnColumnTests
{
    [Fact]
    public void AFillThatReplacesNullsMatchesTheStepwiseRunAfterARawStatement()
    {
        Assert.Equal(Stepwise(RawStatementChain), Collapsed(RawStatementChain));
    }

    [Fact]
    public void AFillThatReplacesNullsMatchesTheStepwiseRunAfterASeedInsert()
    {
        Assert.Equal(Stepwise(SeedInsertChain), Collapsed(SeedInsertChain));
    }

    [Fact]
    public void AFillThatReplacesNullsKeepsEveryRowValue()
    {
        List<(int Id, int? Val)> source = [(1, null), (2, 5), (3, 7)];
        List<(int Id, int Val)> expected = source
            .OrderBy(r => r.Id)
            .Select(r => (Id: r.Id, Val: r.Val ?? 0))
            .ToList();

        Assert.Equal(expected, Collapsed(RawStatementChain));
    }

    private static void RawStatementChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.Sql("INSERT INTO \"H22lOwnNullFill\" (\"Id\", \"Val\") VALUES (3, 7)"));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.TableChanged<H22lOwnNullFillRow>(
                s => s.Set(x => x.Val, x => SQLiteColumn.Of<int?>(x, "Val") ?? 0)));
        }
    }

    private static void SeedInsertChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.Insert(new H22lOwnNullFillRow { Id = 3, Val = 7 }));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.TableChanged<H22lOwnNullFillRow>(
                s => s.Set(x => x.Val, x => SQLiteColumn.Of<int?>(x, "Val") ?? 0)));
        }
    }

    private static List<(int Id, int Val)> Stepwise(Action<SQLiteMigrationRunner, int> chain)
    {
        using TestDatabase db = new(useFile: true);
        Seed(db);
        for (int upTo = 2; upTo <= 3; upTo++)
        {
            SQLiteMigrationRunner runner = db.Schema.Migrations();
            chain(runner, upTo);
            runner.Migrate();
        }

        return Rows(db);
    }

    private static List<(int Id, int Val)> Collapsed(Action<SQLiteMigrationRunner, int> chain)
    {
        using TestDatabase db = new(useFile: true);
        Seed(db);
        SQLiteMigrationRunner runner = db.Schema.Migrations();
        chain(runner, 3);
        runner.Migrate();
        return Rows(db);
    }

    private static void Seed(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"H22lOwnNullFill\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" INTEGER)");
        db.Execute("INSERT INTO \"H22lOwnNullFill\" (\"Id\", \"Val\") VALUES (1, NULL)");
        db.Execute("INSERT INTO \"H22lOwnNullFill\" (\"Id\", \"Val\") VALUES (2, 5)");
        db.Pragmas.UserVersion = 1;
    }

    private static List<(int Id, int Val)> Rows(TestDatabase db)
    {
        return db.Table<H22lOwnNullFillRow>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.Val })
            .ToList()
            .Select(x => (x.Id, x.Val))
            .ToList();
    }
}
