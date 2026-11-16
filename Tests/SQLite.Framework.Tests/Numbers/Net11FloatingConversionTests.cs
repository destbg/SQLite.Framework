#if NET11_0_OR_GREATER
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class Net11FloatingConversionTests
{
    [Theory]
    [InlineData(-70000.75)]
    [InlineData(-298.75)]
    [InlineData(-0.9)]
    [InlineData(42.75)]
    [InlineData(298.75)]
    [InlineData(70000.75)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    public void SmallIntegerCastsSaturateLikeTheRuntime(double value)
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new() { Id = 1, Title = "number", AuthorId = 1, Price = value });
        var expected = new { Byte = (byte)value, SByte = (sbyte)value, Short = (short)value, UShort = (ushort)value };
        var actual = db.Table<Book>().Select(x => new
        {
            Byte = (byte)x.Price, SByte = (sbyte)x.Price, Short = (short)x.Price, UShort = (ushort)x.Price
        }).Single();
        Assert.Equal(expected, actual);
        Assert.Equal(1, db.Table<Book>().Count(x => (byte)x.Price == expected.Byte));
        Assert.Equal((byte)(float)value, db.Table<Book>().Select(x => (byte)(float)x.Price).Single());
        Assert.Equal((short?)value, db.Table<Book>().Select(x => (short?)(double?)x.Price).Single());
    }

    [Theory]
    [InlineData(-70000.75)]
    [InlineData(42.75)]
    [InlineData(70000.75)]
    public void SmallEnumCastsSaturateInProjectionsAndPredicates(double value)
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new() { Id = 1, Title = "number", AuthorId = 1, Price = value });
        var expected = new { Byte = (EfByteEnum)value, SByte = (EfSByteEnum)value, Short = (EfShortEnum)value, UShort = (EfUShortEnum)value };
        var actual = db.Table<Book>().Select(x => new
        {
            Byte = (EfByteEnum)x.Price, SByte = (EfSByteEnum)x.Price,
            Short = (EfShortEnum)x.Price, UShort = (EfUShortEnum)x.Price
        }).Single();
        Assert.Equal(expected, actual);
        Assert.Equal(1, db.Table<Book>().Count(x => (EfByteEnum)x.Price == expected.Byte
            && (EfSByteEnum)x.Price == expected.SByte && (EfShortEnum)x.Price == expected.Short
            && (EfUShortEnum)x.Price == expected.UShort));
        Assert.Equal((EfByteEnum?)(double?)value, db.Table<Book>().Select(x => (EfByteEnum?)(double?)x.Price).Single());
    }

    [Theory]
    [InlineData(CharStorageMode.Integer, -42.0)]
    [InlineData(CharStorageMode.Integer, 65578.0)]
    [InlineData(CharStorageMode.Text, -42.0)]
    [InlineData(CharStorageMode.Text, 65578.0)]
    public void CharCastsSaturateForBothStorageModes(CharStorageMode storage, double value)
    {
        using TestDatabase db = new(x => x.UseCharStorage(storage));
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new() { Id = 1, Title = "number", AuthorId = 1, Price = value });
        Assert.Equal((char)value, db.Table<Book>().Select(x => (char)x.Price).Single());
    }

    [Fact]
    public void CheckedCharCastsRetainFloatingPointConversion()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new() { Id = 1, Title = "number", AuthorId = 1, Price = 66.75 });
        Assert.Equal('B', db.Table<Book>().Select(x => checked((char)x.Price)).Single());
        Assert.Equal('B', db.Table<Book>().Select(x => checked((char)(float)x.Price)).Single());
        Assert.Equal('B', db.Table<Book>().Select(x => (char)(decimal)x.Price).Single());
    }
}
#endif
