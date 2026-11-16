#if NET11_0_OR_GREATER
using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public sealed class Net11JoinRow
{
    [Key]
    public int Id { get; set; }
    public string? Code { get; set; }
}

public class Net11JoinTests
{
    private static readonly Net11JoinRow[] Left =
    [
        new() { Id = 1, Code = "a" }, new() { Id = 2, Code = "a" },
        new() { Id = 3, Code = "left" }, new() { Id = 4 }
    ];
    private static readonly Net11JoinRow[] Right =
    [
        new() { Id = 5, Code = "a" }, new() { Id = 6, Code = "a" },
        new() { Id = 7, Code = "right" }, new() { Id = 8 }
    ];

    private static TestDatabase CreateDatabase()
    {
        TestDatabase db = new();
        db.Table<Net11JoinRow>().Schema.CreateTable();
        db.Table<Net11JoinRow>().AddRange(Left.Concat(Right));
        return db;
    }

    [Fact]
    public void FullJoinSelectorMatchesEnumerableIncludingNullAndDuplicateKeys()
    {
        using TestDatabase db = CreateDatabase();
        var query = db.Table<Net11JoinRow>().Where(x => x.Id < 5)
            .FullJoin(db.Table<Net11JoinRow>().Where(x => x.Id >= 5), l => l.Code, r => r.Code,
                (l, r) => new { Left = l == null ? 0 : l.Id, Right = r == null ? 0 : r.Id });
        var expected = Left.FullJoin(Right, l => l.Code, r => r.Code,
            (l, r) => new { Left = l == null ? 0 : l.Id, Right = r == null ? 0 : r.Id });
        Assert.Contains("FULL OUTER JOIN", query.ToSqlCommand().CommandText);
        Assert.Equal(expected.OrderBy(x => x.Left).ThenBy(x => x.Right), query.OrderBy(x => x.Left).ThenBy(x => x.Right).ToList());
    }

    [Theory]
    [InlineData("Join")]
    [InlineData("LeftJoin")]
    [InlineData("RightJoin")]
    [InlineData("FullJoin")]
    public void TupleJoinsMaterializeAndCompose(string method)
    {
        using TestDatabase db = CreateDatabase();
        IQueryable<Net11JoinRow?> left = db.Table<Net11JoinRow>().Where(x => x.Id < 5);
        IQueryable<Net11JoinRow?> right = db.Table<Net11JoinRow>().Where(x => x.Id >= 5);
        var query = method switch
        {
            "Join" => left.Join(right, l => l!.Code, r => r!.Code),
            "LeftJoin" => left.LeftJoin(right, l => l!.Code, r => r!.Code),
            "RightJoin" => left.RightJoin(right, l => l!.Code, r => r!.Code),
            _ => left.FullJoin(right, l => l!.Code, r => r!.Code)
        };
        IEnumerable<Net11JoinRow?> leftRows = Left;
        IEnumerable<Net11JoinRow?> rightRows = Right;
        var expected = method switch
        {
            "Join" => leftRows.Join(rightRows, l => l!.Code, r => r!.Code),
            "LeftJoin" => leftRows.LeftJoin(rightRows, l => l!.Code, r => r!.Code),
            "RightJoin" => leftRows.RightJoin(rightRows, l => l!.Code, r => r!.Code),
            _ => leftRows.FullJoin(rightRows, l => l!.Code, r => r!.Code)
        };
        var ids = expected.Select(x => (x.Outer?.Id ?? 0, x.Inner?.Id ?? 0)).Order().ToArray();
        Assert.Equal(ids, query.ToList().Select(x => (x.Outer?.Id ?? 0, x.Inner?.Id ?? 0)).Order());
        var composed = query.Select(x => new { Left = x.Outer == null ? 0 : x.Outer.Id, Right = x.Inner == null ? 0 : x.Inner.Id })
            .OrderBy(x => x.Left).ThenBy(x => x.Right).ToList();
        Assert.Equal(ids, composed.Select(x => (x.Left, x.Right)));
    }

    [Fact]
    public void FullJoinRejectsCustomComparer()
    {
        using TestDatabase db = CreateDatabase();
        Assert.Throws<NotSupportedException>(() => db.Table<Net11JoinRow>()
            .FullJoin(db.Table<Net11JoinRow>(), l => l.Code, r => r.Code, StringComparer.OrdinalIgnoreCase).ToList());
    }

    [Fact]
    public void FullJoinWindowProjectionFiltersAfterTheWindow()
    {
        using TestDatabase db = CreateDatabase();
        var rows = db.Table<Net11JoinRow>().Where(x => x.Id < 5)
            .FullJoin(db.Table<Net11JoinRow>().Where(x => x.Id >= 5), l => l.Code, r => r.Code,
                (l, r) => new
                {
                    Left = l == null ? 0 : l.Id,
                    Rank = SQLiteWindowFunctions.RowNumber().Over().OrderBy(l == null ? 0 : l.Id).AsValue()
                }, null)
            .Where(x => x.Rank <= 2).ToList();
        Assert.Equal(2, rows.Count);
        Assert.All(rows, x => Assert.Equal(0, x.Left));
    }

    [Fact]
    public void SelectorFreeGroupJoinFlattensToLeftJoin()
    {
        using TestDatabase db = CreateDatabase();
        var rows = db.Table<Net11JoinRow>().Where(x => x.Id < 5)
            .GroupJoin(db.Table<Net11JoinRow>().Where(x => x.Id >= 5), l => l.Code, r => r.Code)
            .SelectMany(g => g.DefaultIfEmpty(), (g, r) => new { Left = g.Key.Id, Right = r == null ? 0 : r.Id })
            .OrderBy(x => x.Left).ThenBy(x => x.Right).ToList();
        var expected = Left.GroupJoin(Right, l => l.Code, r => r.Code)
            .SelectMany(g => g.DefaultIfEmpty(), (g, r) => new { Left = g.Key.Id, Right = r == null ? 0 : r.Id });
        Assert.Equal(expected.OrderBy(x => x.Left).ThenBy(x => x.Right), rows);
    }

    [Fact]
    public void SelectorFreeGroupJoinFlattensToInnerJoin()
    {
        using TestDatabase db = CreateDatabase();
        var rows = db.Table<Net11JoinRow>().Where(x => x.Id < 5)
            .GroupJoin(db.Table<Net11JoinRow>().Where(x => x.Id >= 5), l => l.Code, r => r.Code)
            .SelectMany(g => g, (g, r) => new { Left = g.Key.Id, Right = r.Id })
            .OrderBy(x => x.Left).ThenBy(x => x.Right).ToList();
        var expected = Left.Join(Right, l => l.Code, r => r.Code, (l, r) => new { Left = l.Id, Right = r.Id });
        Assert.Equal(expected.OrderBy(x => x.Left).ThenBy(x => x.Right), rows);
    }

    [Fact]
    public void SelectorFreeGroupJoinRequiresFlattening()
    {
        using TestDatabase db = CreateDatabase();
        Assert.Throws<NotSupportedException>(() => db.Table<Net11JoinRow>()
            .GroupJoin(db.Table<Net11JoinRow>(), l => l.Code, r => r.Code).ToList());
    }

    [Fact]
    public void GroupJoinRejectsTakeBeforeFlattening()
    {
        using TestDatabase db = CreateDatabase();
        var left = db.Table<Net11JoinRow>().Where(x => x.Id < 5);
        var right = db.Table<Net11JoinRow>().Where(x => x.Id >= 5);

        var explicitGroup = left.GroupJoin(right, l => l.Code, r => r.Code, (l, group) => new { Group = group })
            .SelectMany(x => x.Group.Take(1).DefaultIfEmpty(), (x, r) => r);
        var implicitGroup = left.GroupJoin(right, l => l.Code, r => r.Code)
            .SelectMany(g => g.Take(1).DefaultIfEmpty(), (g, r) => r);

        Assert.Contains("only supported directly on a group join group",
            Assert.Throws<NotSupportedException>(() => explicitGroup.ToList()).Message);
        Assert.Contains("only supported directly on a group join group",
            Assert.Throws<NotSupportedException>(() => implicitGroup.ToList()).Message);
    }

    [Fact]
    public void ScalarTupleAndGroupJoinsPreserveDefaultValues()
    {
        using TestDatabase db = CreateDatabase();
        var left = db.Table<Net11JoinRow>().Where(x => x.Id < 5).Select(x => x.Id + 0);
        var right = db.Table<Net11JoinRow>().Where(x => x.Id >= 5).Select(x => x.Id);
        Assert.Equal([(0, 5), (0, 6), (0, 7), (0, 8), (1, 0), (2, 0), (3, 0), (4, 0)],
            left.FullJoin(right, l => l, r => r).ToList().Order());
        var groups = left.GroupJoin(right, l => l, r => r)
            .SelectMany(g => g.DefaultIfEmpty(), (g, r) => new { Left = g.Key, Right = r }).ToList();
        Assert.Equal([(1, 0), (2, 0), (3, 0), (4, 0)], groups.Select(x => (x.Left, x.Right)).Order());
    }

    [Fact]
    public void SelectorFreeGroupJoinFiltersBeforeFlattening()
    {
        using TestDatabase db = CreateDatabase();
        var rows = db.Table<Net11JoinRow>().Where(x => x.Id < 5)
            .GroupJoin(db.Table<Net11JoinRow>().Where(x => x.Id >= 5), l => l.Code, r => r.Code)
            .SelectMany(g => g.Where(r => r.Id == 5).DefaultIfEmpty(),
                (g, r) => new { Left = g.Key.Id, Right = r == null ? 0 : r.Id })
            .OrderBy(x => x.Left).ToList();
        Assert.Equal([(1, 5), (2, 5), (3, 0), (4, 0)], rows.Select(x => (x.Left, x.Right)));
    }

    [Fact]
    public void SelectorFreeGroupJoinCarriesThroughAnotherJoin()
    {
        using TestDatabase db = CreateDatabase();
        var rows = db.Table<Net11JoinRow>().Where(x => x.Id < 5)
            .GroupJoin(db.Table<Net11JoinRow>().Where(x => x.Id >= 5), l => l.Code, r => r.Code)
            .Join(db.Table<Net11JoinRow>(), g => g.Key.Id, row => row.Id, (g, row) => new { Group = g })
            .SelectMany(x => x.Group.DefaultIfEmpty(),
                (x, r) => new { Left = x.Group.Key.Id, Right = r == null ? 0 : r.Id })
            .OrderBy(x => x.Left).ThenBy(x => x.Right).ToList();
        Assert.Equal([(1, 5), (1, 6), (2, 5), (2, 6), (3, 0), (4, 0)], rows.Select(x => (x.Left, x.Right)));
    }

    [Fact]
    public void ChainedSelectorFreeGroupJoinsKeepBothGroups()
    {
        using TestDatabase db = CreateDatabase();
        var left = db.Table<Net11JoinRow>().Where(x => x.Id < 5);
        var right = db.Table<Net11JoinRow>().Where(x => x.Id >= 5);
        var rows = left.GroupJoin(right, l => l.Code, r => r.Code)
            .GroupJoin(right, g => g.Key.Code, r => r.Code)
            .SelectMany(g => g.Key.DefaultIfEmpty(), (g, first) => new { Group = g, First = first })
            .SelectMany(x => x.Group.DefaultIfEmpty(), (x, second) => new
            {
                Left = x.Group.Key.Key.Id,
                First = x.First == null ? 0 : x.First.Id,
                Second = second == null ? 0 : second.Id
            }).ToList();
        var expected = Left.GroupJoin(Right, l => l.Code, r => r.Code)
            .GroupJoin(Right, g => g.Key.Code, r => r.Code)
            .SelectMany(g => g.Key.DefaultIfEmpty(), (g, first) => new { Group = g, First = first })
            .SelectMany(x => x.Group.DefaultIfEmpty(), (x, second) => new
            {
                Left = x.Group.Key.Key.Id,
                First = x.First == null ? 0 : x.First.Id,
                Second = second == null ? 0 : second.Id
            });
        Assert.Equal(expected.OrderBy(x => x.Left).ThenBy(x => x.First).ThenBy(x => x.Second),
            rows.OrderBy(x => x.Left).ThenBy(x => x.First).ThenBy(x => x.Second));
    }

    [Fact]
    public void SelectorFreeGroupJoinAfterTupleJoinPreservesUnmatchedRows()
    {
        using TestDatabase db = CreateDatabase();
        var rows = db.Table<Net11JoinRow>().Where(x => x.Id < 5)
            .LeftJoin(db.Table<Net11JoinRow>().Where(x => x.Id >= 5), l => l.Code, r => r.Code)
            .GroupJoin(db.Table<Net11JoinRow>(), pair => pair.Outer.Id, row => row.Id)
            .SelectMany(g => g, (g, row) => new
            {
                Left = g.Key.Outer.Id, Right = g.Key.Inner == null ? 0 : g.Key.Inner.Id, Match = row.Id
            }).OrderBy(x => x.Left).ThenBy(x => x.Right).ToList();
        Assert.Equal([(1, 5, 1), (1, 6, 1), (2, 5, 2), (2, 6, 2), (3, 0, 3), (4, 0, 4)],
            rows.Select(x => (x.Left, x.Right, x.Match)));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void FullJoinHandlesEmptyInputs(bool includeLeft, bool includeRight)
    {
        using TestDatabase db = CreateDatabase();
        var actual = db.Table<Net11JoinRow>().Where(x => includeLeft && x.Id < 5)
            .FullJoin(db.Table<Net11JoinRow>().Where(x => includeRight && x.Id >= 5), l => l.Code, r => r.Code,
                (l, r) => new { Left = l == null ? 0 : l.Id, Right = r == null ? 0 : r.Id }, null).ToList();
        var expected = Left.Where(x => includeLeft).FullJoin(Right.Where(x => includeRight), l => l.Code, r => r.Code,
            (l, r) => new { Left = l == null ? 0 : l.Id, Right = r == null ? 0 : r.Id });
        Assert.Equal(expected.OrderBy(x => x.Left).ThenBy(x => x.Right), actual.OrderBy(x => x.Left).ThenBy(x => x.Right));
    }
}
#endif
