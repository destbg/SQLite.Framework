using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ShiftOperatorSemanticsTests
{
    private static TestDatabase SeedInts(params (int id, int value)[] rows)
    {
        TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        foreach ((int id, int value) in rows)
        {
            db.Table<NumericType>().Add(new NumericType { Id = id, IntValue = value });
        }

        return db;
    }

    private static TestDatabase SeedLongs(params (int id, long value)[] rows)
    {
        TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        foreach ((int id, long value) in rows)
        {
            db.Table<NumericType>().Add(new NumericType { Id = id, LongValue = value });
        }

        return db;
    }

    private static TestDatabase SeedUInts(params (int id, uint value)[] rows)
    {
        TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        foreach ((int id, uint value) in rows)
        {
            db.Table<NumericType>().Add(new NumericType { Id = id, UIntValue = value });
        }

        return db;
    }

    [Fact]
    public void IntRightShift_CountMasking_MatchesDotNet()
    {
        (int id, int value)[] rows = [(1, 1024), (2, -8), (3, 256)];
        using TestDatabase db = SeedInts(rows);

        List<int> expected = rows.OrderBy(r => r.id).Select(r => r.value >> 33).ToList();
        List<int> actual = db.Table<NumericType>().OrderBy(x => x.Id).Select(x => x.IntValue >> 33).ToList();

        Assert.Equal([512, -4, 128], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IntLeftShift_SignAndOverflow_MatchesDotNet()
    {
        (int id, int value)[] rows = [(1, 1), (2, 2)];
        using TestDatabase db = SeedInts(rows);

        List<int> expected = rows.OrderBy(r => r.id).Select(r => r.value << 31).ToList();
        List<int> actual = db.Table<NumericType>().OrderBy(x => x.Id).Select(x => x.IntValue << 31).ToList();

        Assert.Equal([-2147483648, 0], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IntLeftShift_CountMasking_MatchesDotNet()
    {
        (int id, int value)[] rows = [(1, 5), (2, 7)];
        using TestDatabase db = SeedInts(rows);

        List<int> expected = rows.OrderBy(r => r.id).Select(r => r.value << 32).ToList();
        List<int> actual = db.Table<NumericType>().OrderBy(x => x.Id).Select(x => x.IntValue << 32).ToList();

        Assert.Equal([5, 7], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IntLeftShift_Truncation_MatchesDotNet()
    {
        (int id, int value)[] rows = [(1, 65536), (2, 1)];
        using TestDatabase db = SeedInts(rows);

        List<int> expected = rows.OrderBy(r => r.id).Select(r => r.value << 16).ToList();
        List<int> actual = db.Table<NumericType>().OrderBy(x => x.Id).Select(x => x.IntValue << 16).ToList();

        Assert.Equal([0, 65536], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IntRightShift_NegativeArithmetic_MatchesDotNet()
    {
        (int id, int value)[] rows = [(1, -16), (2, 17)];
        using TestDatabase db = SeedInts(rows);

        List<int> expected = rows.OrderBy(r => r.id).Select(r => r.value >> 2).ToList();
        List<int> actual = db.Table<NumericType>().OrderBy(x => x.Id).Select(x => x.IntValue >> 2).ToList();

        Assert.Equal([-4, 4], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IntLeftShift_NegativeValue_MatchesDotNet()
    {
        (int id, int value)[] rows = [(1, -1), (2, int.MinValue)];
        using TestDatabase db = SeedInts(rows);

        List<int> expected = rows.OrderBy(r => r.id).Select(r => r.value << 1).ToList();
        List<int> actual = db.Table<NumericType>().OrderBy(x => x.Id).Select(x => x.IntValue << 1).ToList();

        Assert.Equal([-2, 0], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IntLeftShift_InWhere_NegativeRowsKept()
    {
        (int id, int value)[] rows = [(1, 1), (2, 2), (3, 3), (4, 4)];
        using TestDatabase db = SeedInts(rows);

        List<int> expected = rows.Where(r => (r.value << 31) < 0).Select(r => r.id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<NumericType>().Where(x => (x.IntValue << 31) < 0).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal([1, 3], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LongLeftShift_NoTruncationAt64Bit_MatchesDotNet()
    {
        (int id, long value)[] rows = [(1, 1L)];
        using TestDatabase db = SeedLongs(rows);

        long expected = rows[0].value << 40;
        long actual = db.Table<NumericType>().Where(x => x.Id == 1).Select(x => x.LongValue << 40).First();

        Assert.Equal(1099511627776L, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LongLeftShift_CountMasking_MatchesDotNet()
    {
        (int id, long value)[] rows = [(1, 12345L)];
        using TestDatabase db = SeedLongs(rows);

        long expected = rows[0].value << 64;
        long actual = db.Table<NumericType>().Where(x => x.Id == 1).Select(x => x.LongValue << 64).First();

        Assert.Equal(12345L, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LongRightShift_MatchesDotNet()
    {
        (int id, long value)[] rows = [(1, 1099511627776L)];
        using TestDatabase db = SeedLongs(rows);

        long expected = rows[0].value >> 40;
        long actual = db.Table<NumericType>().Where(x => x.Id == 1).Select(x => x.LongValue >> 40).First();

        Assert.Equal(1L, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UIntRightShift_Logical_MatchesDotNet()
    {
        (int id, uint value)[] rows = [(1, 2147483648u)];
        using TestDatabase db = SeedUInts(rows);

        uint expected = rows[0].value >> 1;
        uint actual = db.Table<NumericType>().Where(x => x.Id == 1).Select(x => x.UIntValue >> 1).First();

        Assert.Equal(1073741824u, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UIntLeftShift_Truncation_MatchesDotNet()
    {
        (int id, uint value)[] rows = [(1, 2147483648u), (2, 3u)];
        using TestDatabase db = SeedUInts(rows);

        List<uint> expected = rows.OrderBy(r => r.id).Select(r => r.value << 1).ToList();
        List<uint> actual = db.Table<NumericType>().OrderBy(x => x.Id).Select(x => x.UIntValue << 1).ToList();

        Assert.Equal([0u, 6u], expected);
        Assert.Equal(expected, actual);
    }
}
