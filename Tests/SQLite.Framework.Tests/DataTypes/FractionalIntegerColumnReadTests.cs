using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24rFractionalRows")]
public class H24rFractionalRow
{
    [Key]
    public int Id { get; set; }

    public int Whole { get; set; }

    public short Small { get; set; }

    public long Big { get; set; }

    public int? MaybeWhole { get; set; }

    public bool Flag { get; set; }

    public bool? MaybeFlag { get; set; }
}

[Table("H24rFractionalWideRows")]
public class H24rFractionalWideRow
{
    [Key]
    public int Id { get; set; }

    public long? MaybeBig { get; set; }

    public byte Tiny { get; set; }

    public sbyte SignedTiny { get; set; }

    public ushort UnsignedSmall { get; set; }

    public uint UnsignedWhole { get; set; }

    public ulong UnsignedBig { get; set; }
}

public class FractionalIntegerColumnReadTests
{
    private static H24rFractionalRow ReadRowStoredAs(TestDatabase db, string numberLiteral, string flagLiteral)
    {
        db.Table<H24rFractionalRow>().Schema.CreateTable();
        db.Execute(
            "INSERT INTO \"H24rFractionalRows\" (\"Id\", \"Whole\", \"Small\", \"Big\", \"MaybeWhole\", \"Flag\", \"MaybeFlag\") "
            + $"VALUES (1, {numberLiteral}, {numberLiteral}, {numberLiteral}, {numberLiteral}, {flagLiteral}, {flagLiteral})");

        return db.Table<H24rFractionalRow>().Single();
    }

    [Fact]
    public void PositiveFractionalValueReadsTheSameIntoEveryIntegerMember()
    {
        using TestDatabase db = new();

        H24rFractionalRow row = ReadRowStoredAs(db, "2.7", "1");

        Assert.Equal((int)2.7, row.Whole);
        Assert.Equal((short)2.7, row.Small);
        Assert.Equal((long)2.7, row.Big);
        Assert.Equal((int)2.7, row.MaybeWhole);
    }

    [Fact]
    public void NegativeFractionalValueReadsTheSameIntoEveryIntegerMember()
    {
        using TestDatabase db = new();

        H24rFractionalRow row = ReadRowStoredAs(db, "-2.7", "1");

        Assert.Equal((int)-2.7, row.Whole);
        Assert.Equal((short)-2.7, row.Small);
        Assert.Equal((long)-2.7, row.Big);
        Assert.Equal((int)-2.7, row.MaybeWhole);
    }

    [Fact]
    public void PositiveFractionalValueReadsTheSameIntoEveryWideIntegerMember()
    {
        using TestDatabase db = new();
        db.Table<H24rFractionalWideRow>().Schema.CreateTable();
        db.Execute(
            "INSERT INTO \"H24rFractionalWideRows\" (\"Id\", \"MaybeBig\", \"Tiny\", \"SignedTiny\", \"UnsignedSmall\", \"UnsignedWhole\", \"UnsignedBig\") "
            + "VALUES (1, 2.7, 2.7, 2.7, 2.7, 2.7, 2.7)");

        H24rFractionalWideRow row = db.Table<H24rFractionalWideRow>().Single();

        Assert.Equal((long)2.7, row.MaybeBig);
        Assert.Equal((byte)2.7, row.Tiny);
        Assert.Equal((sbyte)2.7, row.SignedTiny);
        Assert.Equal((ushort)2.7, row.UnsignedSmall);
        Assert.Equal((uint)2.7, row.UnsignedWhole);
        Assert.Equal((ulong)2.7, row.UnsignedBig);
    }

    [Fact]
    public void NonZeroFractionalValueReadsAsTrueInEveryBooleanMember()
    {
        using TestDatabase db = new();

        H24rFractionalRow row = ReadRowStoredAs(db, "0", "0.5");

        Assert.True(row.Flag);
        Assert.True(row.MaybeFlag);
    }
}
