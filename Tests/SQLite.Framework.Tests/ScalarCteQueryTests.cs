using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ScalarCteQueryTests
{
    [Fact]
    public void SelectScalarValues()
    {
        using TestDatabase db = Seed(out List<ScalarCteRow> mem);
        SQLiteCte<int> cte = db.With(() => db.Table<ScalarCteRow>().Select(r => r.Val));

        List<int> expected = mem.Select(r => r.Val).OrderBy(x => x).ToList();
        List<int> actual = (from a in cte select a).ToList().OrderBy(x => x).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WhereOnScalarValue()
    {
        using TestDatabase db = Seed(out List<ScalarCteRow> mem);
        SQLiteCte<int> cte = db.With(() => db.Table<ScalarCteRow>().Select(r => r.Val));

        List<int> expected = mem.Select(r => r.Val).Where(a => a > 15).OrderBy(x => x).ToList();
        List<int> actual = (from a in cte where a > 15 select a).ToList().OrderBy(x => x).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByScalarValue()
    {
        using TestDatabase db = Seed(out List<ScalarCteRow> mem);
        SQLiteCte<int> cte = db.With(() => db.Table<ScalarCteRow>().Select(r => r.Val));

        List<int> expected = mem.Select(r => r.Val).OrderBy(i => i).ToList();
        List<int> actual = (from a in cte select a).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByDescendingScalarValue()
    {
        using TestDatabase db = Seed(out List<ScalarCteRow> mem);
        SQLiteCte<int> cte = db.With(() => db.Table<ScalarCteRow>().Select(r => r.Val));

        List<int> expected = mem.Select(r => r.Val).OrderByDescending(i => i).ToList();
        List<int> actual = (from a in cte select a).OrderByDescending(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ScalarStringCteOrderBy()
    {
        using TestDatabase db = Seed(out List<ScalarCteRow> mem);
        SQLiteCte<string> cte = db.With(() => db.Table<ScalarCteRow>().Select(r => r.Name));

        List<string> expected = mem.Select(r => r.Name).OrderBy(s => s, System.StringComparer.Ordinal).ToList();
        List<string> actual = (from a in cte orderby a select a).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UnionScalarCteOrderBy()
    {
        using TestDatabase db = Seed(out List<ScalarCteRow> mem);
        SQLiteCte<int> cte = db.With(() =>
            db.Table<ScalarCteRow>().Where(r => r.Val < 25).Select(r => r.Val)
                .Union(db.Table<ScalarCteRow>().Where(r => r.Val > 15).Select(r => r.Val)));

        List<int> expected = mem.Where(r => r.Val < 25).Select(r => r.Val)
            .Union(mem.Where(r => r.Val > 15).Select(r => r.Val))
            .OrderBy(i => i).ToList();
        List<int> actual = (from a in cte select a).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    private static TestDatabase Seed(out List<ScalarCteRow> mem)
    {
        TestDatabase db = new();
        db.Table<ScalarCteRow>().Schema.CreateTable();
        mem = new List<ScalarCteRow>
        {
            new() { Id = 1, Val = 30, Name = "cherry" },
            new() { Id = 2, Val = 10, Name = "apple" },
            new() { Id = 3, Val = 20, Name = "banana" },
        };
        foreach (ScalarCteRow r in mem)
        {
            db.Table<ScalarCteRow>().Add(r);
        }

        return db;
    }
}

[Table("ScalarCteRows")]
public class ScalarCteRow
{
    [Key]
    public int Id { get; set; }

    public int Val { get; set; }

    public required string Name { get; set; }
}
