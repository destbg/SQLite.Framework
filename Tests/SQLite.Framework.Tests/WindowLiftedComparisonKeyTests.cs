using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("HsjWinRows")]
public class HsjWinRow
{
    [Key]
    public int Id { get; set; }

    public int? MaybeCount { get; set; }
}

public class HsjWinValueRow
{
    public int Id { get; set; }

    public long Val { get; set; }
}

public class WindowLiftedComparisonKeyTests
{
    private static List<HsjWinRow> Rows()
    {
        return
        [
            new HsjWinRow { Id = 1, MaybeCount = 9 },
            new HsjWinRow { Id = 2, MaybeCount = 3 },
            new HsjWinRow { Id = 3, MaybeCount = null },
            new HsjWinRow { Id = 4, MaybeCount = null }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<HsjWinRow>().Schema.CreateTable();
        db.Table<HsjWinRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void CountPartitionedByNullableComparison()
    {
        using TestDatabase db = Setup();
        List<HsjWinRow> local = Rows();

        Dictionary<bool, long> sizes = local
            .GroupBy(x => x.MaybeCount > 5)
            .ToDictionary(g => g.Key, g => (long)g.Count());
        List<long> expected = local
            .OrderBy(x => x.Id)
            .Select(x => sizes[x.MaybeCount > 5])
            .ToList();

        List<long> actual = db.Table<HsjWinRow>()
            .Select(x => new HsjWinValueRow
            {
                Id = x.Id,
                Val = SQLiteWindowFunctions.Count()
                    .Over()
                    .PartitionBy(x.MaybeCount > 5)
            })
            .ToList()
            .OrderBy(r => r.Id)
            .Select(r => r.Val)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RowNumberOrderedByNullableComparison()
    {
        using TestDatabase db = Setup();
        List<HsjWinRow> local = Rows();

        List<long> expected = local
            .OrderBy(x => x.MaybeCount > 5)
            .ThenBy(x => x.Id)
            .Select((x, i) => new { x.Id, Rn = (long)(i + 1) })
            .OrderBy(a => a.Id)
            .Select(a => a.Rn)
            .ToList();

        List<long> actual = db.Table<HsjWinRow>()
            .Select(x => new HsjWinValueRow
            {
                Id = x.Id,
                Val = SQLiteWindowFunctions.RowNumber()
                    .Over()
                    .OrderBy(x.MaybeCount > 5)
                    .ThenOrderBy(x.Id)
            })
            .ToList()
            .OrderBy(r => r.Id)
            .Select(r => r.Val)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
