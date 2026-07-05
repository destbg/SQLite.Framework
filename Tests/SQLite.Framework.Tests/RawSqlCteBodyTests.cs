using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("RawCteBook")]
public class RawCteBookRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public string Title { get; set; } = "";

    public int Pages { get; set; }
}

public class RawSqlCteBodyTests
{
    [Fact]
    public void FromSqlAsCteBodyMatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<RawCteBookRow>().Schema.CreateTable();
        List<RawCteBookRow> rows =
        [
            new() { Title = "alpha", Pages = 100 },
            new() { Title = "beta", Pages = 200 },
            new() { Title = "gamma", Pages = 300 },
        ];
        foreach (RawCteBookRow row in rows)
        {
            db.Table<RawCteBookRow>().Add(row);
        }

        SQLiteCte<RawCteBookRow> cte = db.With(() => db.FromSql<RawCteBookRow>("SELECT * FROM RawCteBook WHERE Pages > 100"));

        List<string> expected = rows.Where(r => r.Pages > 100).Select(r => r.Title).OrderBy(x => x).ToList();
        List<string> actual = (from b in cte select b.Title).ToList().OrderBy(x => x).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ParameterizedFromSqlAsCteBodyMatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<RawCteBookRow>().Schema.CreateTable();
        List<RawCteBookRow> rows =
        [
            new() { Title = "alpha", Pages = 100 },
            new() { Title = "beta", Pages = 200 },
            new() { Title = "gamma", Pages = 300 },
        ];
        foreach (RawCteBookRow row in rows)
        {
            db.Table<RawCteBookRow>().Add(row);
        }

        SQLiteCte<RawCteBookRow> cte = db.With(() => db.FromSql<RawCteBookRow>(
            "SELECT * FROM RawCteBook WHERE Pages > @pages",
            new SQLiteParameter { Name = "@pages", Value = 100 }));

        List<string> expected = rows.Where(r => r.Pages > 100).Select(r => r.Title).OrderBy(x => x).ToList();
        List<string> actual = (from b in cte select b.Title).ToList().OrderBy(x => x).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FromSqlCteBodyJoinedWithTableMatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<RawCteBookRow>().Schema.CreateTable();
        List<RawCteBookRow> rows =
        [
            new() { Title = "alpha", Pages = 100 },
            new() { Title = "beta", Pages = 200 },
            new() { Title = "gamma", Pages = 300 },
        ];
        foreach (RawCteBookRow row in rows)
        {
            db.Table<RawCteBookRow>().Add(row);
        }

        SQLiteCte<RawCteBookRow> cte = db.With(() => db.FromSql<RawCteBookRow>("SELECT * FROM RawCteBook WHERE Pages >= 200"));

        List<string> expected = rows
            .Where(r => r.Pages >= 200)
            .Join(rows, c => c.Id, b => b.Id, (c, b) => c.Title + b.Pages)
            .OrderBy(x => x)
            .ToList();
        List<string> actual = cte
            .Join(db.Table<RawCteBookRow>(), c => c.Id, b => b.Id, (c, b) => c.Title + b.Pages)
            .ToList()
            .OrderBy(x => x)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
