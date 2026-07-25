using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21bCteSideRow")]
public class H21bCteSideRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }

    public int B { get; set; }
}

[Table("H21bCteSideTag")]
public class H21bCteSideTag
{
    [Key]
    public int Id { get; set; }

    public int RowId { get; set; }

    public string Name { get; set; } = "";
}

public class CteClientMemberJoinSideColumnTests
{
    private static List<H21bCteSideRow> Rows() =>
    [
        new H21bCteSideRow { Id = 1, A = 10, B = 100 },
        new H21bCteSideRow { Id = 2, A = 20, B = 200 },
        new H21bCteSideRow { Id = 3, A = 30, B = 300 },
    ];

    private static List<H21bCteSideTag> Tags() =>
    [
        new H21bCteSideTag { Id = 1, RowId = 1, Name = "one" },
        new H21bCteSideTag { Id = 2, RowId = 2, Name = "two" },
        new H21bCteSideTag { Id = 3, RowId = 9, Name = "none" },
    ];

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H21bCteSideRow>().Schema.CreateTable();
        db.Table<H21bCteSideRow>().AddRange(Rows());
        db.Table<H21bCteSideTag>().Schema.CreateTable();
        db.Table<H21bCteSideTag>().AddRange(Tags());
        return db;
    }

    [Fact]
    public void CteWithArrayMemberAsJoinSideMatchesLinq()
    {
        using TestDatabase db = Setup();

        var memory = Rows().Select(r => new { r.Id, Arr = new[] { r.A, r.B } }).ToList();
        List<string> expected = Tags()
            .Join(memory, t => t.RowId, x => x.Id, (t, x) => t.Name)
            .OrderBy(s => s)
            .ToList();

        var cte = db.With(() => db.Table<H21bCteSideRow>()
            .Select(r => new { r.Id, Arr = new[] { r.A, r.B } }));

        List<string> actual = db.Table<H21bCteSideTag>()
            .Join(cte, t => t.RowId, x => x.Id, (t, x) => t.Name)
            .AsEnumerable()
            .OrderBy(s => s)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CteWithArrayMemberOnBothSelectManySourcesMatchesLinq()
    {
        using TestDatabase db = Setup();

        var memory = Rows().Select(r => new { r.Id, Arr = new[] { r.A, r.B } }).ToList();
        List<(int X, int Y)> expected = memory
            .SelectMany(x => memory, (x, y) => new { X = x.Id, Y = y.Id })
            .Select(p => (p.X, p.Y))
            .OrderBy(p => p.X)
            .ThenBy(p => p.Y)
            .ToList();

        var cte = db.With(() => db.Table<H21bCteSideRow>()
            .Select(r => new { r.Id, Arr = new[] { r.A, r.B } }));

        List<(int X, int Y)> actual = cte
            .SelectMany(x => cte, (x, y) => new { X = x.Id, Y = y.Id })
            .AsEnumerable()
            .Select(p => (p.X, p.Y))
            .OrderBy(p => p.X)
            .ThenBy(p => p.Y)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CteWithArrayMemberAsSelectManySourceMatchesLinq()
    {
        using TestDatabase db = Setup();

        var memory = Rows().Select(r => new { r.Id, Arr = new[] { r.A, r.B } }).ToList();
        List<(string Name, int Id)> expected = Tags()
            .SelectMany(t => memory, (t, x) => new { t.Name, x.Id })
            .Select(p => (p.Name, p.Id))
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .ToList();

        var cte = db.With(() => db.Table<H21bCteSideRow>()
            .Select(r => new { r.Id, Arr = new[] { r.A, r.B } }));

        List<(string Name, int Id)> actual = db.Table<H21bCteSideTag>()
            .SelectMany(t => cte, (t, x) => new { t.Name, x.Id })
            .AsEnumerable()
            .Select(p => (p.Name, p.Id))
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
