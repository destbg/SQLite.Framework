using System;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class NarrowReadRow
{
    [Key]
    public int Id { get; set; }

    public short ShortValue { get; set; }

    public ushort UShortValue { get; set; }

    public byte ByteValue { get; set; }

    public sbyte SByteValue { get; set; }
}

public class NarrowPropertyOutOfRangeReadParityTests
{
    [Fact]
    public void OutOfRangeNarrowColumns_ThrowOverflowOnRead()
    {
        using TestDatabase db = new();
        db.Table<NarrowReadRow>().Schema.CreateTable();
        db.Execute(
            "INSERT INTO \"NarrowReadRow\" (\"Id\", \"ShortValue\", \"UShortValue\", \"ByteValue\", \"SByteValue\") VALUES (1, 40000, 70000, 300, 200)");

        Assert.Throws<OverflowException>(() => db.Table<NarrowReadRow>().Select(r => r.ShortValue).ToList());
        Assert.Throws<OverflowException>(() => db.Table<NarrowReadRow>().Select(r => r.UShortValue).ToList());
        Assert.Throws<OverflowException>(() => db.Table<NarrowReadRow>().Select(r => r.ByteValue).ToList());
        Assert.Throws<OverflowException>(() => db.Table<NarrowReadRow>().Select(r => r.SByteValue).ToList());
        Assert.Throws<OverflowException>(() => db.Table<NarrowReadRow>().Single());
    }

    [Fact]
    public void InRangeNarrowColumns_RoundTrip()
    {
        using TestDatabase db = new();
        db.Table<NarrowReadRow>().Schema.CreateTable();
        db.Table<NarrowReadRow>().Add(new NarrowReadRow
        {
            Id = 1,
            ShortValue = -5,
            UShortValue = 60000,
            ByteValue = 200,
            SByteValue = -100,
        });

        NarrowReadRow row = db.Table<NarrowReadRow>().Single();

        Assert.Equal((short)-5, row.ShortValue);
        Assert.Equal((ushort)60000, row.UShortValue);
        Assert.Equal((byte)200, row.ByteValue);
        Assert.Equal((sbyte)-100, row.SByteValue);
    }
}
