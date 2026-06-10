using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class NarrowTypeConstantConversionParityTests
{
    private static TestDatabase CreateByteDb(byte value)
    {
        TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, ByteValue = value });
        return db;
    }

    private static TestDatabase CreateUShortDb(ushort value)
    {
        TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, UShortValue = value });
        return db;
    }

    private static TestDatabase CreateSByteDb(sbyte value)
    {
        TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, SByteValue = value });
        return db;
    }

    [Fact]
    public void ByteBitwiseNot_CapturedVar_MatchesDotNet()
    {
        using TestDatabase db = CreateByteDb(5);
        byte b = 5;

        bool expected = 5 == (byte)~b;
        bool actual = db.Table<NumericType>().Select(x => x.ByteValue == (byte)~b).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ByteNegate_CapturedVar_MatchesDotNet()
    {
        using TestDatabase db = CreateByteDb(5);
        byte b = 5;

        bool expected = 5 == (byte)-b;
        bool actual = db.Table<NumericType>().Select(x => x.ByteValue == (byte)-b).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UShortBitwiseNot_CapturedVar_MatchesDotNet()
    {
        using TestDatabase db = CreateUShortDb(5);
        ushort us = 5;

        bool expected = 5 == (ushort)~us;
        bool actual = db.Table<NumericType>().Select(x => x.UShortValue == (ushort)~us).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SByteNegate_MinValue_CapturedVar_MatchesDotNet()
    {
        using TestDatabase db = CreateSByteDb(sbyte.MinValue);
        sbyte sb = sbyte.MinValue;

        bool expected = sbyte.MinValue == (sbyte)-sb;
        bool actual = db.Table<NumericType>().Select(x => x.SByteValue == (sbyte)-sb).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LongToInt_OutOfRange_CapturedVar_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, IntValue = unchecked((int)long.MaxValue) });
        long l = long.MaxValue;

        bool expected = unchecked((int)long.MaxValue) == (int)l;
        bool actual = db.Table<NumericType>().Select(x => x.IntValue == (int)l).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LongToShort_OutOfRange_CapturedVar_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, ShortValue = unchecked((short)long.MaxValue) });
        long l = long.MaxValue;

        bool expected = unchecked((short)long.MaxValue) == (short)l;
        bool actual = db.Table<NumericType>().Select(x => x.ShortValue == (short)l).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UIntToInt_OutOfRange_CapturedVar_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, IntValue = unchecked((int)uint.MaxValue) });
        uint u = uint.MaxValue;

        bool expected = unchecked((int)uint.MaxValue) == (int)u;
        bool actual = db.Table<NumericType>().Select(x => x.IntValue == (int)u).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ULongToLong_OutOfRange_CapturedVar_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, LongValue = unchecked((long)ulong.MaxValue) });
        ulong ul = ulong.MaxValue;

        bool expected = unchecked((long)ulong.MaxValue) == (long)ul;
        bool actual = db.Table<NumericType>().Select(x => x.LongValue == (long)ul).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IntToUInt_Negative_CapturedVar_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, UIntValue = unchecked((uint)-1) });
        int i = -1;

        bool expected = unchecked((uint)-1) == (uint)i;
        bool actual = db.Table<NumericType>().Select(x => x.UIntValue == (uint)i).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LongToULong_Negative_CapturedVar_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, ULongValue = unchecked((ulong)-1L) });
        long l = -1;

        bool expected = unchecked((ulong)-1L) == (ulong)l;
        bool actual = db.Table<NumericType>().Select(x => x.ULongValue == (ulong)l).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IntThroughChar_OutOfRange_CapturedVar_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, IntValue = unchecked((char)70000) });
        int i = 70000;

        bool expected = unchecked((char)70000) == (int)(char)i;
        bool actual = db.Table<NumericType>().Select(x => x.IntValue == (int)(char)i).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CharToLong_CapturedVar_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, LongValue = 'h' });
        char c = 'h';

        bool expected = 'h' == (long)c;
        bool actual = db.Table<NumericType>().Select(x => x.LongValue == (long)c).First();

        Assert.Equal(expected, actual);
    }
}
