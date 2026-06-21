using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class CompositeJoinKeyRow
{
    [Key]
    public int Id { get; set; }

    public int? A { get; set; }

    public string? Name { get; set; }
}

public class CompositeJoinNullStringKeyParityTests
{
    private static readonly CompositeJoinKeyRow[] Rows =
    [
        new CompositeJoinKeyRow { Id = 1, A = 5, Name = null },
        new CompositeJoinKeyRow { Id = 2, A = 5, Name = null },
        new CompositeJoinKeyRow { Id = 3, A = 9, Name = "x" }
    ];

    [Fact]
    public void CompositeJoinWithNullStringComponent_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<CompositeJoinKeyRow>().Schema.CreateTable();
        foreach (CompositeJoinKeyRow row in Rows)
        {
            db.Table<CompositeJoinKeyRow>().Add(row);
        }

        List<(int, int)> expected = (from x in Rows
                join y in Rows on new { x.A, x.Name } equals new { y.A, y.Name }
                select new { XId = x.Id, YId = y.Id })
            .Select(p => (p.XId, p.YId))
            .OrderBy(p => p.Item1).ThenBy(p => p.Item2)
            .ToList();

        List<(int, int)> actual = (from x in db.Table<CompositeJoinKeyRow>()
                join y in db.Table<CompositeJoinKeyRow>() on new { x.A, x.Name } equals new { y.A, y.Name }
                select new { XId = x.Id, YId = y.Id })
            .ToList()
            .Select(p => (p.XId, p.YId))
            .OrderBy(p => p.Item1).ThenBy(p => p.Item2)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
