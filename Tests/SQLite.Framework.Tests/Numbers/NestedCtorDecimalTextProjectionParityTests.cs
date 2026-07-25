using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class NestedCtorDecimalTextProjectionParityTests
{
    public class NestedDecSource
    {
        [Key]
        public int Id { get; set; }

        public decimal Price { get; set; }
    }

    public record NestedDecRec(decimal Amount);

    private static readonly NestedDecSource[] SeedRows =
    [
        new NestedDecSource { Id = 1, Price = 1234567890.1234567890m },
        new NestedDecSource { Id = 2, Price = 9876543210.9876543210m },
    ];

    private static TestDatabase Create()
    {
        TestDatabase db = new(b => b.DecimalStorage = DecimalStorageMode.Text);
        db.Table<NestedDecSource>().Schema.CreateTable();
        foreach (NestedDecSource r in SeedRows)
        {
            db.Table<NestedDecSource>().Add(r);
        }
        return db;
    }

    [Fact]
    public void DirectDecimalProjection_RoundTripsExactly()
    {
        using TestDatabase db = Create();

        List<decimal> oracle = SeedRows.OrderBy(x => x.Id).Select(x => x.Price).ToList();
        List<decimal> actual = db.Table<NestedDecSource>().OrderBy(x => x.Id).Select(x => x.Price).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void NestedRecordDecimalMemberInSecondSelect_MatchesLinqToObjects()
    {
        using TestDatabase db = Create();

        List<decimal> oracle = SeedRows
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id, R = new NestedDecRec(x.Price) })
            .Select(a => a.R.Amount)
            .ToList();

        List<decimal> actual = db.Table<NestedDecSource>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id, R = new NestedDecRec(x.Price) })
            .Select(a => a.R.Amount)
            .ToList();

        Assert.Equal(oracle, actual);
    }
}
