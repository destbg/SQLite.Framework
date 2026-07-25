using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class LiftedOperandRow
{
    [Key]
    public int Id { get; set; }

    public int Amount { get; set; }
}

public class ClientEvalLiftedNullOperandTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Table<LiftedOperandRow>().Schema.CreateTable();
        db.Table<LiftedOperandRow>().Add(new LiftedOperandRow { Id = 1, Amount = 5 });
        db.Table<LiftedOperandRow>().Add(new LiftedOperandRow { Id = 2, Amount = 2 });
        return db;
    }

    [Fact]
    public void NullGreaterThanValueIsFalse()
    {
        using TestDatabase db = SetupDatabase();

        List<bool> expected = db.Table<LiftedOperandRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => ClientEvalTestFunctions.ToNullable(r.Amount) > ClientEvalTestFunctions.PassNullable(4))
            .ToList();

        Assert.Equal([true, false], expected);

        List<bool> actual = db.Table<LiftedOperandRow>()
            .OrderBy(r => r.Id)
            .Select(r => ClientEvalTestFunctions.ToNullable(r.Amount) > ClientEvalTestFunctions.PassNullable(4))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ValueGreaterThanNullIsFalse()
    {
        using TestDatabase db = SetupDatabase();

        List<bool> expected = db.Table<LiftedOperandRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => ClientEvalTestFunctions.PassNullable(4) > ClientEvalTestFunctions.ToNullable(r.Amount))
            .ToList();

        Assert.Equal([false, false], expected);

        List<bool> actual = db.Table<LiftedOperandRow>()
            .OrderBy(r => r.Id)
            .Select(r => ClientEvalTestFunctions.PassNullable(4) > ClientEvalTestFunctions.ToNullable(r.Amount))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullPlusValueIsNull()
    {
        using TestDatabase db = SetupDatabase();

        List<int?> expected = db.Table<LiftedOperandRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => ClientEvalTestFunctions.ToNullable(r.Amount) + ClientEvalTestFunctions.PassNullable(4))
            .ToList();

        Assert.Equal([9, null], expected);

        List<int?> actual = db.Table<LiftedOperandRow>()
            .OrderBy(r => r.Id)
            .Select(r => ClientEvalTestFunctions.ToNullable(r.Amount) + ClientEvalTestFunctions.PassNullable(4))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullDecimalTimesValueIsNull()
    {
        using TestDatabase db = SetupDatabase();

        List<decimal?> expected = db.Table<LiftedOperandRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => ClientEvalTestFunctions.ToNullableDecimal(r.Amount) * 2m)
            .ToList();

        Assert.Equal([10m, null], expected);

        List<decimal?> actual = db.Table<LiftedOperandRow>()
            .OrderBy(r => r.Id)
            .Select(r => ClientEvalTestFunctions.ToNullableDecimal(r.Amount) * 2m)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullDateTimePlusTimeSpanIsNull()
    {
        using TestDatabase db = SetupDatabase();

        List<DateTime?> expected = db.Table<LiftedOperandRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => ClientEvalTestFunctions.ToNullableDate(r.Amount) + TimeSpan.FromDays(1))
            .ToList();

        Assert.Equal([new DateTime(2020, 1, 6), null], expected);

        List<DateTime?> actual = db.Table<LiftedOperandRow>()
            .OrderBy(r => r.Id)
            .Select(r => ClientEvalTestFunctions.ToNullableDate(r.Amount) + TimeSpan.FromDays(1))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NegateOfNullDecimalIsNull()
    {
        using TestDatabase db = SetupDatabase();

        List<decimal?> expected = db.Table<LiftedOperandRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => -ClientEvalTestFunctions.ToNullableDecimal(r.Amount))
            .ToList();

        Assert.Equal([-5m, null], expected);

        List<decimal?> actual = db.Table<LiftedOperandRow>()
            .OrderBy(r => r.Id)
            .Select(r => -ClientEvalTestFunctions.ToNullableDecimal(r.Amount))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NotOfNullBoolIsNull()
    {
        using TestDatabase db = SetupDatabase();

        List<bool?> expected = db.Table<LiftedOperandRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => !ClientEvalTestFunctions.ToNullableBool(r.Amount))
            .ToList();

        Assert.Equal([false, null], expected);

        List<bool?> actual = db.Table<LiftedOperandRow>()
            .OrderBy(r => r.Id)
            .Select(r => !ClientEvalTestFunctions.ToNullableBool(r.Amount))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableBoolAndValueFollowsThreeValuedLogic()
    {
        using TestDatabase db = SetupDatabase();

        List<bool?> expected = db.Table<LiftedOperandRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => ClientEvalTestFunctions.ToNullableBool(r.Amount) & ClientEvalTestFunctions.ToNullableBool(1))
            .ToList();

        Assert.Equal([true, null], expected);

        List<bool?> actual = db.Table<LiftedOperandRow>()
            .OrderBy(r => r.Id)
            .Select(r => ClientEvalTestFunctions.ToNullableBool(r.Amount) & ClientEvalTestFunctions.ToNullableBool(1))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
