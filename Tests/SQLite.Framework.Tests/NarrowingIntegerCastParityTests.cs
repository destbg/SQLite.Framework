using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class NarrowingIntegerCastParityTests
{
    [Fact]
    public void NonNullableIntToByte_Wrapping_InSelectMatchesObjects()
    {
        using TestDatabase db = new();
        db.Table<NarrowingCastIntRow>().Schema.CreateTable();
        db.Table<NarrowingCastIntRow>().Add(new NarrowingCastIntRow { Id = 1, IntValue = 300 });
        db.Table<NarrowingCastIntRow>().Add(new NarrowingCastIntRow { Id = 2, IntValue = -200 });

        List<(int Id, int IntValue)> seed = new() { (1, 300), (2, -200) };

        List<byte> expected = seed.OrderBy(r => r.Id).Select(r => (byte)r.IntValue).ToList();
        List<byte> actual = db.Table<NarrowingCastIntRow>().OrderBy(r => r.Id).Select(r => (byte)r.IntValue).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NonNullableLongToShort_Wrapping_InSelectMatchesObjects()
    {
        using TestDatabase db = new();
        db.Table<NarrowingCastIntRow>().Schema.CreateTable();
        db.Table<NarrowingCastIntRow>().Add(new NarrowingCastIntRow { Id = 1, LongValue = 4294967297L });

        List<(int Id, long LongValue)> seed = new() { (1, 4294967297L) };

        List<short> expected = seed.Select(r => (short)r.LongValue).ToList();
        List<short> actual = db.Table<NarrowingCastIntRow>().Select(r => (short)r.LongValue).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableIntToByte_Wrapping_InSelectMatchesObjects()
    {
        using TestDatabase db = new();
        db.Table<NarrowingCastNullableRow>().Schema.CreateTable();
        db.Table<NarrowingCastNullableRow>().Add(new NarrowingCastNullableRow { Id = 1, NullableInt = 300 });
        db.Table<NarrowingCastNullableRow>().Add(new NarrowingCastNullableRow { Id = 2, NullableInt = null });

        List<(int Id, int? NullableInt)> seed = new() { (1, 300), (2, null) };

        List<byte?> expected = seed.OrderBy(r => r.Id).Select(r => (byte?)r.NullableInt).ToList();
        List<byte?> actual = db.Table<NarrowingCastNullableRow>().OrderBy(r => r.Id).Select(r => (byte?)r.NullableInt).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableIntToByte_Wrapping_InWhereMatchesObjects()
    {
        using TestDatabase db = new();
        db.Table<NarrowingCastNullableRow>().Schema.CreateTable();
        db.Table<NarrowingCastNullableRow>().Add(new NarrowingCastNullableRow { Id = 1, NullableInt = 300 });
        db.Table<NarrowingCastNullableRow>().Add(new NarrowingCastNullableRow { Id = 2, NullableInt = null });

        List<(int Id, int? NullableInt)> seed = new() { (1, 300), (2, null) };
        byte target = unchecked((byte)300);

        List<int> expected = seed.Where(r => (byte?)r.NullableInt == target).Select(r => r.Id).OrderBy(id => id).ToList();
        List<int> actual = db.Table<NarrowingCastNullableRow>().Where(r => (byte?)r.NullableInt == target).Select(r => r.Id).OrderBy(id => id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableIntToSByte_NegativeWrapping_InSelectMatchesObjects()
    {
        using TestDatabase db = new();
        db.Table<NarrowingCastNullableRow>().Schema.CreateTable();
        db.Table<NarrowingCastNullableRow>().Add(new NarrowingCastNullableRow { Id = 1, NullableInt = -200 });

        List<(int Id, int? NullableInt)> seed = new() { (1, -200) };

        List<sbyte?> expected = seed.Select(r => (sbyte?)r.NullableInt).ToList();
        List<sbyte?> actual = db.Table<NarrowingCastNullableRow>().Select(r => (sbyte?)r.NullableInt).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableIntToShort_Wrapping_InSelectMatchesObjects()
    {
        using TestDatabase db = new();
        db.Table<NarrowingCastNullableRow>().Schema.CreateTable();
        db.Table<NarrowingCastNullableRow>().Add(new NarrowingCastNullableRow { Id = 1, NullableInt = 70000 });

        List<(int Id, int? NullableInt)> seed = new() { (1, 70000) };

        List<short?> expected = seed.Select(r => (short?)r.NullableInt).ToList();
        List<short?> actual = db.Table<NarrowingCastNullableRow>().Select(r => (short?)r.NullableInt).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableIntToUShort_Wrapping_InWhereMatchesObjects()
    {
        using TestDatabase db = new();
        db.Table<NarrowingCastNullableRow>().Schema.CreateTable();
        db.Table<NarrowingCastNullableRow>().Add(new NarrowingCastNullableRow { Id = 1, NullableInt = 70000 });

        List<(int Id, int? NullableInt)> seed = new() { (1, 70000) };
        ushort target = unchecked((ushort)70000);

        List<int> expected = seed.Where(r => (ushort?)r.NullableInt == target).Select(r => r.Id).ToList();
        List<int> actual = db.Table<NarrowingCastNullableRow>().Where(r => (ushort?)r.NullableInt == target).Select(r => r.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableIntToUInt_NegativeValue_InWhereMatchesObjects()
    {
        using TestDatabase db = new();
        db.Table<NarrowingCastNullableRow>().Schema.CreateTable();
        db.Table<NarrowingCastNullableRow>().Add(new NarrowingCastNullableRow { Id = 1, NullableInt = -1 });

        List<(int Id, int? NullableInt)> seed = new() { (1, -1) };
        uint target = 4294967295U;

        List<int> expected = seed.Where(r => (uint?)r.NullableInt == target).Select(r => r.Id).ToList();
        List<int> actual = db.Table<NarrowingCastNullableRow>().Where(r => (uint?)r.NullableInt == target).Select(r => r.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableLongToInt_Wrapping_InSelectMatchesObjects()
    {
        using TestDatabase db = new();
        db.Table<NarrowingCastNullableRow>().Schema.CreateTable();
        db.Table<NarrowingCastNullableRow>().Add(new NarrowingCastNullableRow { Id = 1, NullableLong = 4294967297L });
        db.Table<NarrowingCastNullableRow>().Add(new NarrowingCastNullableRow { Id = 2, NullableLong = null });

        List<(int Id, long? NullableLong)> seed = new() { (1, 4294967297L), (2, null) };

        List<int?> expected = seed.OrderBy(r => r.Id).Select(r => (int?)r.NullableLong).ToList();
        List<int?> actual = db.Table<NarrowingCastNullableRow>().OrderBy(r => r.Id).Select(r => (int?)r.NullableLong).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableLongToInt_Wrapping_InWhereMatchesObjects()
    {
        using TestDatabase db = new();
        db.Table<NarrowingCastNullableRow>().Schema.CreateTable();
        db.Table<NarrowingCastNullableRow>().Add(new NarrowingCastNullableRow { Id = 1, NullableLong = 4294967297L });

        List<(int Id, long? NullableLong)> seed = new() { (1, 4294967297L) };

        List<int> expected = seed.Where(r => (int?)r.NullableLong == 1).Select(r => r.Id).ToList();
        List<int> actual = db.Table<NarrowingCastNullableRow>().Where(r => (int?)r.NullableLong == 1).Select(r => r.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableArithmeticResultToByte_Wrapping_InSelectMatchesObjects()
    {
        using TestDatabase db = new();
        db.Table<NarrowingCastNullableRow>().Schema.CreateTable();
        db.Table<NarrowingCastNullableRow>().Add(new NarrowingCastNullableRow { Id = 1, NullableInt = 70000, NullableIntB = 50000 });
        db.Table<NarrowingCastNullableRow>().Add(new NarrowingCastNullableRow { Id = 2, NullableInt = null, NullableIntB = 5 });

        List<(int Id, int? A, int? B)> seed = new() { (1, 70000, 50000), (2, null, 5) };

        List<byte?> expected = seed.OrderBy(r => r.Id).Select(r => (byte?)(r.A + r.B)).ToList();
        List<byte?> actual = db.Table<NarrowingCastNullableRow>().OrderBy(r => r.Id).Select(r => (byte?)(r.NullableInt + r.NullableIntB)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IntToByteBackedEnum_OutOfRange_InWhereMatchesObjects()
    {
        using TestDatabase db = new();
        db.Table<NarrowingCastIntRow>().Schema.CreateTable();
        db.Table<NarrowingCastIntRow>().Add(new NarrowingCastIntRow { Id = 1, IntValue = 300 });
        db.Table<NarrowingCastIntRow>().Add(new NarrowingCastIntRow { Id = 2, IntValue = 44 });

        List<(int Id, int IntValue)> seed = new() { (1, 300), (2, 44) };

        List<int> expected = seed.Where(r => (NarrowingCastByteEnum)r.IntValue == NarrowingCastByteEnum.Wrapped).Select(r => r.Id).OrderBy(id => id).ToList();
        List<int> actual = db.Table<NarrowingCastIntRow>().Where(r => (NarrowingCastByteEnum)r.IntValue == NarrowingCastByteEnum.Wrapped).Select(r => r.Id).OrderBy(id => id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IntToByteBackedEnum_OutOfRange_InProjectionMatchesObjects()
    {
        using TestDatabase db = new();
        db.Table<NarrowingCastIntRow>().Schema.CreateTable();
        db.Table<NarrowingCastIntRow>().Add(new NarrowingCastIntRow { Id = 1, IntValue = 300 });

        List<(int Id, int IntValue)> seed = new() { (1, 300) };

        List<bool> expected = seed.OrderBy(r => r.Id).Select(r => (NarrowingCastByteEnum)r.IntValue == NarrowingCastByteEnum.Wrapped).ToList();
        List<bool> actual = db.Table<NarrowingCastIntRow>().OrderBy(r => r.Id).Select(r => (NarrowingCastByteEnum)r.IntValue == NarrowingCastByteEnum.Wrapped).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IntToShortBackedEnum_OutOfRange_InWhereMatchesObjects()
    {
        using TestDatabase db = new();
        db.Table<NarrowingCastIntRow>().Schema.CreateTable();
        db.Table<NarrowingCastIntRow>().Add(new NarrowingCastIntRow { Id = 1, IntValue = 70000 });
        db.Table<NarrowingCastIntRow>().Add(new NarrowingCastIntRow { Id = 2, IntValue = 4464 });

        List<(int Id, int IntValue)> seed = new() { (1, 70000), (2, 4464) };

        List<int> expected = seed.Where(r => (NarrowingCastShortEnum)r.IntValue == NarrowingCastShortEnum.Wrapped).Select(r => r.Id).OrderBy(id => id).ToList();
        List<int> actual = db.Table<NarrowingCastIntRow>().Where(r => (NarrowingCastShortEnum)r.IntValue == NarrowingCastShortEnum.Wrapped).Select(r => r.Id).OrderBy(id => id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableIntInRangeNarrowing_NoWrapNeeded_InSelectMatchesObjects()
    {
        using TestDatabase db = new();
        db.Table<NarrowingCastNullableRow>().Schema.CreateTable();
        db.Table<NarrowingCastNullableRow>().Add(new NarrowingCastNullableRow { Id = 1, NullableInt = 100 });
        db.Table<NarrowingCastNullableRow>().Add(new NarrowingCastNullableRow { Id = 2, NullableInt = null });

        List<(int Id, int? NullableInt)> seed = new() { (1, 100), (2, null) };

        List<byte?> expected = seed.OrderBy(r => r.Id).Select(r => (byte?)r.NullableInt).ToList();
        List<byte?> actual = db.Table<NarrowingCastNullableRow>().OrderBy(r => r.Id).Select(r => (byte?)r.NullableInt).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableIntWideningToLong_NullRow_InSelectMatchesObjects()
    {
        using TestDatabase db = new();
        db.Table<NarrowingCastNullableRow>().Schema.CreateTable();
        db.Table<NarrowingCastNullableRow>().Add(new NarrowingCastNullableRow { Id = 1, NullableInt = 100 });
        db.Table<NarrowingCastNullableRow>().Add(new NarrowingCastNullableRow { Id = 2, NullableInt = null });

        List<(int Id, int? NullableInt)> seed = new() { (1, 100), (2, null) };

        List<long?> expected = seed.OrderBy(r => r.Id).Select(r => (long?)r.NullableInt).ToList();
        List<long?> actual = db.Table<NarrowingCastNullableRow>().OrderBy(r => r.Id).Select(r => (long?)r.NullableInt).ToList();

        Assert.Equal(expected, actual);
    }
}

[Table("NarrowingCastNullableRows")]
public class NarrowingCastNullableRow
{
    [Key]
    public int Id { get; set; }

    public int? NullableInt { get; set; }

    public int? NullableIntB { get; set; }

    public long? NullableLong { get; set; }
}

[Table("NarrowingCastIntRows")]
public class NarrowingCastIntRow
{
    [Key]
    public int Id { get; set; }

    public int IntValue { get; set; }

    public long LongValue { get; set; }

    public byte ByteValue { get; set; }
}

public enum NarrowingCastByteEnum : byte
{
    Wrapped = 44
}

public enum NarrowingCastShortEnum : short
{
    Wrapped = 4464
}
