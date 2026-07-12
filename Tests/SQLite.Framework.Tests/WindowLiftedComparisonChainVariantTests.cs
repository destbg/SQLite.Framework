using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("WlcvRows")]
public class WlcvRow
{
    [Key]
    public int Id { get; set; }

    public int? MaybeCount { get; set; }
}

public class WlcvValueRow
{
    public int Id { get; set; }

    public long Val { get; set; }
}

public class WindowLiftedComparisonChainVariantTests
{
    private static List<WlcvRow> Rows()
    {
        return
        [
            new WlcvRow { Id = 1, MaybeCount = 9 },
            new WlcvRow { Id = 2, MaybeCount = 3 },
            new WlcvRow { Id = 3, MaybeCount = null },
            new WlcvRow { Id = 4, MaybeCount = 7 },
            new WlcvRow { Id = 5, MaybeCount = null }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<WlcvRow>().Schema.CreateTable();
        db.Table<WlcvRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void CountThenPartitionedByNullableComparison()
    {
        using TestDatabase db = Setup();
        List<WlcvRow> local = Rows();

        Dictionary<(int, bool), long> sizes = local
            .GroupBy(x => (x.Id % 2, x.MaybeCount > 5))
            .ToDictionary(g => g.Key, g => (long)g.Count());
        List<long> expected = local
            .OrderBy(x => x.Id)
            .Select(x => sizes[(x.Id % 2, x.MaybeCount > 5)])
            .ToList();

        List<long> actual = db.Table<WlcvRow>()
            .Select(x => new WlcvValueRow
            {
                Id = x.Id,
                Val = SQLiteWindowFunctions.Count()
                    .Over()
                    .PartitionBy(x.Id % 2)
                    .ThenPartitionBy(x.MaybeCount > 5)
            })
            .ToList()
            .OrderBy(r => r.Id)
            .Select(r => r.Val)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RowNumberOrderedByDescendingNullableComparison()
    {
        using TestDatabase db = Setup();
        List<WlcvRow> local = Rows();

        List<long> expected = local
            .OrderByDescending(x => x.MaybeCount > 5)
            .ThenBy(x => x.Id)
            .Select((x, i) => new { x.Id, Rn = (long)(i + 1) })
            .OrderBy(a => a.Id)
            .Select(a => a.Rn)
            .ToList();

        List<long> actual = db.Table<WlcvRow>()
            .Select(x => new WlcvValueRow
            {
                Id = x.Id,
                Val = SQLiteWindowFunctions.RowNumber()
                    .Over()
                    .OrderByDescending(x.MaybeCount > 5)
                    .ThenOrderBy(x.Id)
            })
            .ToList()
            .OrderBy(r => r.Id)
            .Select(r => r.Val)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RowNumberThenOrderedByDescendingNullableComparison()
    {
        using TestDatabase db = Setup();
        List<WlcvRow> local = Rows();

        List<long> expected = local
            .OrderBy(x => x.Id % 2)
            .ThenByDescending(x => x.MaybeCount > 5)
            .ThenBy(x => x.Id)
            .Select((x, i) => new { x.Id, Rn = (long)(i + 1) })
            .OrderBy(a => a.Id)
            .Select(a => a.Rn)
            .ToList();

        List<long> actual = db.Table<WlcvRow>()
            .Select(x => new WlcvValueRow
            {
                Id = x.Id,
                Val = SQLiteWindowFunctions.RowNumber()
                    .Over()
                    .OrderBy(x.Id % 2)
                    .ThenOrderByDescending(x.MaybeCount > 5)
                    .ThenOrderBy(x.Id)
            })
            .ToList()
            .OrderBy(r => r.Id)
            .Select(r => r.Val)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
