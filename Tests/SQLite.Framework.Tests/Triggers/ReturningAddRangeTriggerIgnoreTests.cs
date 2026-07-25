using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ReturnAutoRow")]
file sealed class ReturnAutoRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public required string Name { get; set; }
}

public class ReturningAddRangeTriggerIgnoreTests
{
    private static TestDatabase CreateDb()
    {
        TestDatabase db = new();
        db.Table<ReturnAutoRow>().Schema.CreateTable();
        db.Execute("""
            CREATE TRIGGER trg_returning_ignore BEFORE INSERT ON ReturnAutoRow
            FOR EACH ROW
            WHEN NEW.Name = 'skip'
            BEGIN
                SELECT RAISE(IGNORE);
            END;
            """);
        return db;
    }

    [Fact]
    public void IgnoreMiddleRow_SkipsRowAndBackfillsIds()
    {
        using TestDatabase db = CreateDb();

        List<ReturnAutoRow> rows = db.Table<ReturnAutoRow>().Returning().AddRange(
        [
            new ReturnAutoRow { Name = "a" },
            new ReturnAutoRow { Name = "skip" },
            new ReturnAutoRow { Name = "b" },
        ]);

        Assert.Equal(["a", "b"], rows.Select(r => r.Name).ToArray());
        Assert.All(rows, r => Assert.True(r.Id > 0));
        List<(int Id, string Name)> inDb = db.Table<ReturnAutoRow>().OrderBy(r => r.Id)
            .Select(r => new { r.Id, r.Name }).ToList().Select(r => (r.Id, r.Name)).ToList();
        Assert.Equal(rows.Select(r => (r.Id, r.Name)).OrderBy(r => r.Id).ToList(), inDb);
    }

    [Fact]
    public void IgnoreFirstRow_SkipsRow()
    {
        using TestDatabase db = CreateDb();

        List<ReturnAutoRow> rows = db.Table<ReturnAutoRow>().Returning().AddRange(
        [
            new ReturnAutoRow { Name = "skip" },
            new ReturnAutoRow { Name = "a" },
            new ReturnAutoRow { Name = "b" },
        ]);

        Assert.Equal(["a", "b"], rows.Select(r => r.Name).ToArray());
    }

    [Fact]
    public void IgnoreLastRow_SkipsRow()
    {
        using TestDatabase db = CreateDb();

        List<ReturnAutoRow> rows = db.Table<ReturnAutoRow>().Returning().AddRange(
        [
            new ReturnAutoRow { Name = "a" },
            new ReturnAutoRow { Name = "b" },
            new ReturnAutoRow { Name = "skip" },
        ]);

        Assert.Equal(["a", "b"], rows.Select(r => r.Name).ToArray());
    }

    [Fact]
    public void IgnoreAllRows_ReturnsEmptyAndDoesNotThrow()
    {
        using TestDatabase db = CreateDb();

        List<ReturnAutoRow> rows = db.Table<ReturnAutoRow>().Returning().AddRange(
        [
            new ReturnAutoRow { Name = "skip" },
            new ReturnAutoRow { Name = "skip" },
        ]);

        Assert.Empty(rows);
        Assert.Empty(db.Table<ReturnAutoRow>().ToList());
    }

    [Fact]
    public void NoRowsIgnored_AllReturnedAndBackfilled()
    {
        using TestDatabase db = CreateDb();

        List<ReturnAutoRow> rows = db.Table<ReturnAutoRow>().Returning().AddRange(
        [
            new ReturnAutoRow { Name = "a" },
            new ReturnAutoRow { Name = "b" },
            new ReturnAutoRow { Name = "c" },
        ]);

        Assert.Equal(["a", "b", "c"], rows.Select(r => r.Name).ToArray());
        Assert.All(rows, r => Assert.True(r.Id > 0));
        Assert.Equal(rows.Count, rows.Select(r => r.Id).Distinct().Count());
    }

    [Fact]
    public void SingleAdd_IgnoredRow_ReturnsDefault()
    {
        using TestDatabase db = CreateDb();

        ReturnAutoRow? row = db.Table<ReturnAutoRow>().Returning().Add(new ReturnAutoRow { Name = "skip" });

        Assert.Null(row);
        Assert.Empty(db.Table<ReturnAutoRow>().ToList());
    }
}
