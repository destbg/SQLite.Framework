using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class BlobQueryParityTests
{
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
    public void BlobLongLengthInFilter()
    {
        using TestDatabase db = new();
        List<DbbEdgeRow> rows = Seed(db);
        List<int> expected = rows.Where(x => x.Data != null && x.Data.LongLength == 1).OrderBy(x => x.Id).Select(x => x.Id).ToList();
        List<int> actual = db.Table<DbbEdgeRow>().Where(x => x.Data != null && x.Data.LongLength == 1).OrderBy(x => x.Id).Select(x => x.Id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BlobElementAccessByIndex()
    {
        using TestDatabase db = new();
        List<DbbEdgeRow> rows = Seed(db);

        byte expected = rows.Where(x => x.Id == 3).Select(x => x.Data![0]).First();
        byte actual = db.Table<DbbEdgeRow>().Where(x => x.Id == 3).Select(x => x.Data![0]).First();

        Assert.Equal(expected, actual);
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
        List<DbbEdgeRow> rows = Seed(db);

        foreach (byte target in new byte[] { 0, 2, 127, 128, 254, 255 })
        {
            List<int> expected = rows.Where(x => x.Data!.Contains(target)).OrderBy(x => x.Id).Select(x => x.Id).ToList();
            List<int> actual = db.Table<DbbEdgeRow>().Where(x => x.Data!.Contains(target)).OrderBy(x => x.Id).Select(x => x.Id).ToList();
            Assert.Equal(expected, actual);
        }

        byte sqlTarget = 255;
        SQLiteCommand command = db.Table<DbbEdgeRow>()
            .Where(x => x.Data!.Contains(sqlTarget))
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .ToSqlCommand();
        Assert.Equal(new byte[] { 255 }, Assert.IsType<byte[]>(command.Parameters[0].Value));
        Assert.Equal("SELECT d0.\"Id\" AS \"Id\"\nFROM \"DbbEdgeRows\" AS d0\nWHERE INSTR(d0.\"Data\", @p0) > 0\nORDER BY d0.\"Id\" ASC", command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void BlobContainsByteFromColumnMatchesObjects()
    {
        using TestDatabase db = new();
        List<DbbEdgeRow> rows = Seed(db);

        List<int> expected = rows.Where(x => x.Data!.Contains(x.Needle)).OrderBy(x => x.Id).Select(x => x.Id).ToList();
        IQueryable<int> query = db.Table<DbbEdgeRow>().Where(x => x.Data!.Contains(x.Needle)).OrderBy(x => x.Id).Select(x => x.Id);

        Assert.Equal(expected, query.ToList());

        SQLiteCommand command = query.ToSqlCommand();
        Assert.Equal(Enumerable.Range(0, 256).Select(i => (byte)i).ToArray(), Assert.IsType<byte[]>(command.Parameters[0].Value));
        Assert.Equal("SELECT d0.\"Id\" AS \"Id\"\nFROM \"DbbEdgeRows\" AS d0\nWHERE INSTR(d0.\"Data\", SUBSTR(@p0, d0.\"Needle\" + 1, 1)) > 0\nORDER BY d0.\"Id\" ASC", command.CommandText.Replace("\r\n", "\n"));
    }

    private static List<DbbEdgeRow> Seed(SQLiteDatabase db)
    {
        db.Table<DbbEdgeRow>().Schema.CreateTable();
        List<DbbEdgeRow> rows = new()
        {
            new DbbEdgeRow { Id = 1, Amount = 1.005m, Number = 1.5, Whole = 7, Flag = true, NFlag = null, Data = new byte[] { 1, 2, 3 }, Needle = 2 },
            new DbbEdgeRow { Id = 2, Amount = 2.675m, Number = 2.5, Whole = -3, Flag = false, NFlag = true, Data = new byte[] { 1, 2 }, Needle = 3 },
            new DbbEdgeRow { Id = 3, Amount = -2.5m, Number = 0.0, Whole = 0, Flag = true, NFlag = false, Data = new byte[] { 4 }, Needle = 4 },
            new DbbEdgeRow { Id = 4, Amount = 0.1m, Number = 100.0, Whole = 5, Flag = false, NFlag = null, Data = Array.Empty<byte>(), Needle = 0 },
            new DbbEdgeRow { Id = 5, Amount = 0m, Number = -1.0, Whole = 1, Flag = true, NFlag = null, Data = new byte[] { 0, 127, 128, 255 }, Needle = 255 },
            new DbbEdgeRow { Id = 6, Amount = 0m, Number = 0.0, Whole = 0, Flag = false, NFlag = null, Data = new byte[] { 0x12, 0x34 }, Needle = 0x23 },
            new DbbEdgeRow { Id = 7, Amount = 0m, Number = 0.0, Whole = 0, Flag = false, NFlag = null, Data = new byte[] { 0x23 }, Needle = 0x23 }
        };
        foreach (DbbEdgeRow r in rows)
        {
            db.Table<DbbEdgeRow>().Add(r);
        }

        return rows;
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

    public byte Needle { get; set; }
}
