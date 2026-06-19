using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class BlobQueryParityTests
{
    private static List<DbbEdgeRow> Seed(SQLiteDatabase db)
    {
        db.Table<DbbEdgeRow>().Schema.CreateTable();
        List<DbbEdgeRow> rows = new()
        {
            new DbbEdgeRow { Id = 1, Amount = 1.005m, Number = 1.5, Whole = 7, Flag = true, NFlag = null, Data = new byte[] { 1, 2, 3 } },
            new DbbEdgeRow { Id = 2, Amount = 2.675m, Number = 2.5, Whole = -3, Flag = false, NFlag = true, Data = new byte[] { 1, 2 } },
            new DbbEdgeRow { Id = 3, Amount = -2.5m, Number = 0.0, Whole = 0, Flag = true, NFlag = false, Data = new byte[] { 4 } },
            new DbbEdgeRow { Id = 4, Amount = 0.1m, Number = 100.0, Whole = 5, Flag = false, NFlag = null, Data = Array.Empty<byte>() },
        };
        foreach (DbbEdgeRow r in rows)
        {
            db.Table<DbbEdgeRow>().Add(r);
        }

        return rows;
    }

    [Fact]
    public void BlobLengthInProjection()
    {
        using TestDatabase db = new();
        List<DbbEdgeRow> rows = Seed(db);
        List<int> expected = rows.OrderBy(x => x.Id).Select(x => x.Data!.Length).ToList();
        List<int> actual = db.Table<DbbEdgeRow>().OrderBy(x => x.Id).Select(x => x.Data!.Length).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BlobLengthInFilter()
    {
        using TestDatabase db = new();
        List<DbbEdgeRow> rows = Seed(db);
        List<int> expected = rows.Where(x => x.Data != null && x.Data.Length == 1).OrderBy(x => x.Id).Select(x => x.Id).ToList();
        List<int> actual = db.Table<DbbEdgeRow>().Where(x => x.Data != null && x.Data.Length == 1).OrderBy(x => x.Id).Select(x => x.Id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BlobLongLengthInProjection()
    {
        using TestDatabase db = new();
        List<DbbEdgeRow> rows = Seed(db);
        long expected = rows.Where(x => x.Id == 1).Select(x => x.Data!.LongLength).First();
        long actual = db.Table<DbbEdgeRow>().Where(x => x.Id == 1).Select(x => x.Data!.LongLength).First();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BlobElementAccessByIndex()
    {
        using TestDatabase db = new();
        Seed(db);
        Exception ex = Assert.ThrowsAny<Exception>(() =>
            db.Table<DbbEdgeRow>().Where(x => x.Id == 3).Select(x => x.Data![0]).First());
        Assert.True(ex is NotSupportedException or InvalidOperationException);
    }

    [Fact]
    public void BlobSequenceEqualInFilter()
    {
        using TestDatabase db = new();
        List<DbbEdgeRow> rows = Seed(db);
        byte[] target = { 1, 2, 3 };
        List<int> expected = rows.Where(x => x.Data!.SequenceEqual(target)).OrderBy(x => x.Id).Select(x => x.Id).ToList();
        List<int> actual = db.Table<DbbEdgeRow>().Where(x => x.Data!.SequenceEqual(target)).OrderBy(x => x.Id).Select(x => x.Id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BlobContainsByteInFilter()
    {
        using TestDatabase db = new();
        Seed(db);
        Assert.Throws<System.NotSupportedException>(() =>
            db.Table<DbbEdgeRow>().Where(x => x.Data!.Contains((byte)2)).OrderBy(x => x.Id).Select(x => x.Id).ToList());
    }
}

[Table("DbbEdgeRows")]
public class DbbEdgeRow
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("Amount")]
    public decimal Amount { get; set; }

    [Column("Number")]
    public double Number { get; set; }

    [Column("Whole")]
    public int Whole { get; set; }

    [Column("Flag")]
    public bool Flag { get; set; }

    [Column("NFlag")]
    public bool? NFlag { get; set; }

    [Column("Data")]
    public byte[]? Data { get; set; }
}
