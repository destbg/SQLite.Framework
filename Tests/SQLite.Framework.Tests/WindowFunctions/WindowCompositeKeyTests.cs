using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using SQLite.Framework.Extensions;
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
        new WckRow { Id = 1, GroupA = 2, GroupB = 1, Value = 10 },
        new WckRow { Id = 2, GroupA = 1, GroupB = 2, Value = 20 },
        new WckRow { Id = 3, GroupA = 1, GroupB = 1, Value = 30 },
        new WckRow { Id = 4, GroupA = 1, GroupB = 1, Value = 40 },
    ];

    [Fact]
    public void CompositePartitionByKeyMatchesChainedKeys()
    {
        using TestDatabase db = new();
        db.Table<WckRow>().Schema.CreateTable();
        db.Table<WckRow>().AddRange(Rows);

        Dictionary<int, long> expected = Rows
            .GroupBy(r => new { r.GroupA, r.GroupB })
            .SelectMany(g => g.OrderBy(r => r.Id).Select((r, i) => new { r.Id, Rn = (long)(i + 1) }))
            .ToDictionary(x => x.Id, x => x.Rn);

        IQueryable<long> query = db.Table<WckRow>()
            .OrderBy(r => r.Id)
            .Select(r => SQLiteWindowFunctions.RowNumber().Over().PartitionBy(new { r.GroupA, r.GroupB }).OrderBy(r.Id).AsValue());
        List<long> values = query.ToList();
        Dictionary<int, long> actual = Rows.OrderBy(r => r.Id)
            .Select((r, i) => new { r.Id, Rn = values[i] })
            .ToDictionary(x => x.Id, x => x.Rn);

        Assert.Equal(expected, actual);

        SQLiteCommand command = query.ToSqlCommand();
        Assert.Equal("SELECT ROW_NUMBER() OVER ( PARTITION BY w0.\"GroupA\", w0.\"GroupB\" ORDER BY w0.\"Id\" ASC) AS \"10\"\nFROM \"WckRow\" AS w0\nORDER BY w0.\"Id\" ASC", command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void CompositeOrderByKeyMatchesChainedKeys()
    {
        using TestDatabase db = new();
        db.Table<WckRow>().Schema.CreateTable();
        db.Table<WckRow>().AddRange(Rows);

        Dictionary<int, long> expected = Rows
            .OrderBy(r => r.GroupA)
            .ThenBy(r => r.GroupB)
            .ThenBy(r => r.Id)
            .Select((r, i) => new { r.Id, Rn = (long)(i + 1) })
            .ToDictionary(x => x.Id, x => x.Rn);

        Dictionary<int, long> actual = db.Table<WckRow>()
            .Select(r => new
            {
                r.Id,
                Rn = SQLiteWindowFunctions.RowNumber().Over().OrderBy(new { r.GroupA, r.GroupB }).ThenOrderBy(r.Id).AsValue()
            })
            .ToList()
            .ToDictionary(x => x.Id, x => x.Rn);

        Assert.Equal(expected, actual);

        SQLiteCommand command = db.Table<WckRow>()
            .Select(r => SQLiteWindowFunctions.RowNumber().Over().OrderBy(new { r.GroupA, r.GroupB }).ThenOrderBy(r.Id).AsValue())
            .ToSqlCommand();
        Assert.Equal("SELECT ROW_NUMBER() OVER ( ORDER BY w0.\"GroupA\" ASC, w0.\"GroupB\" ASC, w0.\"Id\" ASC) AS \"9\"\nFROM \"WckRow\" AS w0", command.CommandText.Replace("\r\n", "\n"));
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

    [Fact]
    public void CompositeKeyExpressionsCarryParameters()
    {
        using TestDatabase db = new();
        db.Table<WckRow>().Schema.CreateTable();
        db.Table<WckRow>().AddRange(Rows);
        int offset = 5;

        Dictionary<int, long> expected = Rows
            .GroupBy(r => new { r.GroupA, Key = r.GroupB + offset })
            .SelectMany(g => g.OrderBy(r => r.Id).Select((r, i) => new { r.Id, Rn = (long)(i + 1) }))
            .ToDictionary(x => x.Id, x => x.Rn);
        Dictionary<int, long> actual = db.Table<WckRow>()
            .Select(r => new
            {
                r.Id,
                Rn = SQLiteWindowFunctions.RowNumber().Over().PartitionBy(new { r.GroupA, Key = r.GroupB + offset }).OrderBy(r.Id).AsValue()
            })
            .ToList()
            .ToDictionary(x => x.Id, x => x.Rn);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CompositeThenPartitionByKeyMatchesObjects()
    {
        using TestDatabase db = new();
        db.Table<WckRow>().Schema.CreateTable();
        db.Table<WckRow>().AddRange(Rows);

        Dictionary<int, long> expected = Rows
            .GroupBy(r => new { r.GroupA, r.GroupB, High = r.Value > 20 })
            .SelectMany(g => g.OrderBy(r => r.Id).Select((r, i) => new { r.Id, Rn = (long)(i + 1) }))
            .ToDictionary(x => x.Id, x => x.Rn);
        Dictionary<int, long> actual = db.Table<WckRow>()
            .Select(r => new
            {
                r.Id,
                Rn = SQLiteWindowFunctions.RowNumber().Over()
                    .PartitionBy(r.GroupA)
                    .ThenPartitionBy(new { r.GroupB, High = r.Value > 20 })
                    .OrderBy(r.Id)
                    .AsValue()
            })
            .ToList()
            .ToDictionary(x => x.Id, x => x.Rn);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CompositeOrderByDescendingKeyMatchesObjects()
    {
        using TestDatabase db = new();
        db.Table<WckRow>().Schema.CreateTable();
        db.Table<WckRow>().AddRange(Rows);

        Dictionary<int, long> expected = Rows
            .OrderByDescending(r => r.GroupA)
            .ThenByDescending(r => r.GroupB)
            .ThenByDescending(r => r.Id)
            .Select((r, i) => new { r.Id, Rn = (long)(i + 1) })
            .ToDictionary(x => x.Id, x => x.Rn);
        Dictionary<int, long> actual = db.Table<WckRow>()
            .Select(r => new
            {
                r.Id,
                Rn = SQLiteWindowFunctions.RowNumber().Over()
                    .OrderByDescending(new { r.GroupA, r.GroupB })
                    .ThenOrderByDescending(r.Id)
                    .AsValue()
            })
            .ToList()
            .ToDictionary(x => x.Id, x => x.Rn);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CompositeThenOrderByKeyMatchesObjects()
    {
        using TestDatabase db = new();
        db.Table<WckRow>().Schema.CreateTable();
        db.Table<WckRow>().AddRange(Rows);

        Dictionary<int, long> expected = Rows
            .OrderBy(r => r.GroupA)
            .ThenBy(r => r.GroupB)
            .ThenBy(r => r.Value)
            .Select((r, i) => new { r.Id, Rn = (long)(i + 1) })
            .ToDictionary(x => x.Id, x => x.Rn);
        Dictionary<int, long> actual = db.Table<WckRow>()
            .Select(r => new
            {
                r.Id,
                Rn = SQLiteWindowFunctions.RowNumber().Over()
                    .OrderBy(r.GroupA)
                    .ThenOrderBy(new { r.GroupB, r.Value })
                    .AsValue()
            })
            .ToList()
            .ToDictionary(x => x.Id, x => x.Rn);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CompositeThenOrderByDescendingKeyMatchesObjects()
    {
        using TestDatabase db = new();
        db.Table<WckRow>().Schema.CreateTable();
        db.Table<WckRow>().AddRange(Rows);

        Dictionary<int, long> expected = Rows
            .OrderBy(r => r.GroupA)
            .ThenByDescending(r => r.GroupB)
            .ThenByDescending(r => r.Value)
            .Select((r, i) => new { r.Id, Rn = (long)(i + 1) })
            .ToDictionary(x => x.Id, x => x.Rn);
        Dictionary<int, long> actual = db.Table<WckRow>()
            .Select(r => new
            {
                r.Id,
                Rn = SQLiteWindowFunctions.RowNumber().Over()
                    .OrderBy(r.GroupA)
                    .ThenOrderByDescending(new { r.GroupB, r.Value })
                    .AsValue()
            })
            .ToList()
            .ToDictionary(x => x.Id, x => x.Rn);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConstantCompositePartitionKeyMatchesObjects()
    {
        using TestDatabase db = new();
        db.Table<WckRow>().Schema.CreateTable();
        db.Table<WckRow>().AddRange(Rows);

        List<long> expected = Rows.OrderBy(r => r.Id).Select((_, i) => (long)(i + 1)).ToList();
        List<long> actual = db.Table<WckRow>()
            .OrderBy(r => r.Id)
            .Select(r => SQLiteWindowFunctions.RowNumber().Over().PartitionBy(new { First = 1, Second = 2 }).OrderBy(r.Id).AsValue())
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CompositeKeyWithClientValueThrows()
    {
        using TestDatabase db = new();
        db.Table<WckRow>().Schema.CreateTable();

        Assert.Throws<NotSupportedException>(() => db.Table<WckRow>()
            .Select(r => SQLiteWindowFunctions.RowNumber().Over().OrderBy(new { r.GroupA, Value = Identity(r.Value) }).AsValue())
            .ToList());
        Assert.Throws<NotSupportedException>(() => db.Table<WckRow>()
            .Select(r => SQLiteWindowFunctions.RowNumber().Over().OrderBy(new[] { r.GroupA, r.GroupB }).AsValue())
            .ToList());
    }

    private static int Identity(int value)
    {
        return value;
    }
}
