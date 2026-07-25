using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class WckRow
{
    [Key]
    public int Id { get; set; }

    public int GroupA { get; set; }

    public int GroupB { get; set; }

    public int Value { get; set; }
}

public class WindowCompositeKeyTests
{
    private static readonly WckRow[] Rows =
    [
        new WckRow { Id = 1, GroupA = 1, GroupB = 1, Value = 10 },
        new WckRow { Id = 2, GroupA = 1, GroupB = 1, Value = 20 },
        new WckRow { Id = 3, GroupA = 1, GroupB = 2, Value = 30 },
        new WckRow { Id = 4, GroupA = 2, GroupB = 1, Value = 40 },
    ];

    [Fact]
    public void CompositePartitionByKey_ThrowsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<WckRow>().Schema.CreateTable();
        db.Table<WckRow>().AddRange(Rows);

        Assert.Throws<NotSupportedException>(() =>
            db.Table<WckRow>()
                .Select(r => SQLiteWindowFunctions.RowNumber().Over().PartitionBy(new { r.GroupA, r.GroupB }).OrderBy(r.Id).AsValue())
                .ToList());
    }

    [Fact]
    public void CompositeOrderByKey_ThrowsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<WckRow>().Schema.CreateTable();
        db.Table<WckRow>().AddRange(Rows);

        Assert.Throws<NotSupportedException>(() =>
            db.Table<WckRow>()
                .Select(r => SQLiteWindowFunctions.RowNumber().Over().OrderBy(new { r.GroupA, r.GroupB }).AsValue())
                .ToList());
    }

    [Fact]
    public void ChainedPartitionByKeys_MatchesObjects()
    {
        using TestDatabase db = new();
        db.Table<WckRow>().Schema.CreateTable();
        db.Table<WckRow>().AddRange(Rows);

        Dictionary<int, long> expected = Rows
            .GroupBy(r => new { r.GroupA, r.GroupB })
            .SelectMany(g => g.OrderBy(r => r.Id).Select((r, i) => new { r.Id, Rn = (long)(i + 1) }))
            .ToDictionary(x => x.Id, x => x.Rn);

        Dictionary<int, long> actual = db.Table<WckRow>()
            .Select(r => new { r.Id, Rn = SQLiteWindowFunctions.RowNumber().Over().PartitionBy(r.GroupA).ThenPartitionBy(r.GroupB).OrderBy(r.Id).AsValue() })
            .ToList()
            .ToDictionary(x => x.Id, x => x.Rn);

        Assert.Equal(expected, actual);
    }
}
