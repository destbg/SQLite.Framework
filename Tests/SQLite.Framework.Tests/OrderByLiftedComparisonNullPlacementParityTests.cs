using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class LiftedOrderRow
{
    [Key] public int Id { get; set; }
    public int? A { get; set; }
}

public class OrderByLiftedComparisonNullPlacementParityTests
{
    [Fact]
    public void OrderBy_NullableRelationalComparison_PlacesNullRowsWithFalseGroup()
    {
        using TestDatabase db = new();
        db.Table<LiftedOrderRow>().Schema.CreateTable();
        List<LiftedOrderRow> rows = new()
        {
            new() { Id = 1, A = 1 },
            new() { Id = 2, A = null },
            new() { Id = 3, A = 10 },
        };
        foreach (LiftedOrderRow r in rows)
        {
            db.Table<LiftedOrderRow>().Add(r);
        }

        List<int> expected = rows.OrderBy(r => r.A > 5).ThenBy(r => r.Id).Select(r => r.Id).ToList();
        List<int> actual = db.Table<LiftedOrderRow>().OrderBy(r => r.A > 5).ThenBy(r => r.Id).Select(r => r.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByDescending_NullableRelationalComparison_PlacesNullRowsWithFalseGroup()
    {
        using TestDatabase db = new();
        db.Table<LiftedOrderRow>().Schema.CreateTable();
        List<LiftedOrderRow> rows = new()
        {
            new() { Id = 1, A = null },
            new() { Id = 2, A = 1 },
            new() { Id = 3, A = 10 },
        };
        foreach (LiftedOrderRow r in rows)
        {
            db.Table<LiftedOrderRow>().Add(r);
        }

        List<int> expected = rows.OrderByDescending(r => r.A > 5).ThenBy(r => r.Id).Select(r => r.Id).ToList();
        List<int> actual = db.Table<LiftedOrderRow>().OrderByDescending(r => r.A > 5).ThenBy(r => r.Id).Select(r => r.Id).ToList();

        Assert.Equal(expected, actual);
    }
}
