using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class ClientEvalSourceRow
{
    [Key]
    public int Id { get; set; }

    public int Amount { get; set; }
}

public static class ClientEvalTestFunctions
{
    public static int? ToNullable(int value)
    {
        return value == 2 ? null : value;
    }

    public static int? PassNullable(int? value)
    {
        return value;
    }

    public static int Pass(int value)
    {
        return value;
    }

    public static long PassLong(long value)
    {
        return value;
    }

    public static decimal? ToNullableDecimal(int value)
    {
        return value == 2 ? null : value;
    }

    public static DateTime? ToNullableDate(int value)
    {
        return value == 2 ? null : new DateTime(2020, 1, value);
    }

    public static bool? ToNullableBool(int value)
    {
        return value == 2 ? null : true;
    }
}

public class ClientEvalReceiverMemberProjectionTests
{
    private static TestDatabase SetupDatabase(params int[] amounts)
    {
        TestDatabase db = new();
        db.Table<ClientEvalSourceRow>().Schema.CreateTable();
        int id = 1;
        foreach (int amount in amounts)
        {
            db.Table<ClientEvalSourceRow>().Add(new ClientEvalSourceRow { Id = id++, Amount = amount });
        }
        return db;
    }

    [Fact]
    public void DivRemQuotientReturnsTheQuotient()
    {
        using TestDatabase db = SetupDatabase(23);

        int expected = db.Table<ClientEvalSourceRow>().AsEnumerable()
            .Select(r => Math.DivRem(r.Amount, 7).Quotient)
            .First();

        Assert.Equal(3, expected);

        int actual = db.Table<ClientEvalSourceRow>()
            .Select(r => Math.DivRem(r.Amount, 7).Quotient)
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DivRemRemainderReturnsTheRemainder()
    {
        using TestDatabase db = SetupDatabase(23);

        int expected = db.Table<ClientEvalSourceRow>().AsEnumerable()
            .Select(r => Math.DivRem(r.Amount, 7).Remainder)
            .First();

        Assert.Equal(2, expected);

        int actual = db.Table<ClientEvalSourceRow>()
            .Select(r => Math.DivRem(r.Amount, 7).Remainder)
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void HasValueOnClientMethodResultIsEvaluated()
    {
        using TestDatabase db = SetupDatabase(1, 2);

        List<bool> expected = db.Table<ClientEvalSourceRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => ClientEvalTestFunctions.ToNullable(r.Amount).HasValue)
            .ToList();

        Assert.Equal([true, false], expected);

        List<bool> actual = db.Table<ClientEvalSourceRow>()
            .OrderBy(r => r.Id)
            .Select(r => ClientEvalTestFunctions.ToNullable(r.Amount).HasValue)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
