using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24lClientCastRows")]
public class H24lClientCastRow
{
    [Key]
    public int Id { get; set; }

    public int Amount { get; set; }
}

public readonly struct H24lMoney
{
    public H24lMoney(int cents)
    {
        Cents = cents;
    }

    public int Cents { get; }

    public static explicit operator int(H24lMoney money)
    {
        return money.Cents / 100;
    }
}

public static class H24lClientCastFunctions
{
    public static int Pass(int value)
    {
        return value;
    }

    public static H24lMoney ToMoney(int value)
    {
        return new H24lMoney(value * 100 + 250);
    }

    public static H24lMoney? MaybeMoney(int value)
    {
        return value > 100 ? new H24lMoney(value) : null;
    }
}

public class ClientEvalConversionOperandParityTests
{
    [Fact]
    public void ProjectsAnOutOfRangeClientIntCastToChar()
    {
        using TestDatabase db = Seed();
        List<H24lClientCastRow> local = Rows();

        List<char> expected = local
            .OrderBy(r => r.Id)
            .Select(r => (char)H24lClientCastFunctions.Pass(r.Amount))
            .ToList();

        List<char> actual = db.Table<H24lClientCastRow>()
            .OrderBy(r => r.Id)
            .Select(r => (char)H24lClientCastFunctions.Pass(r.Amount))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProjectsAClientValueThroughItsOwnConversionOperator()
    {
        using TestDatabase db = Seed();
        List<H24lClientCastRow> local = Rows();

        List<int> expected = local
            .OrderBy(r => r.Id)
            .Select(r => (int)H24lClientCastFunctions.ToMoney(r.Amount))
            .ToList();

        List<int> actual = db.Table<H24lClientCastRow>()
            .OrderBy(r => r.Id)
            .Select(r => (int)H24lClientCastFunctions.ToMoney(r.Amount))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProjectsAClientValueThroughACheckedConversionOperator()
    {
        using TestDatabase db = Seed();
        List<H24lClientCastRow> local = Rows();

        List<int?> expected = local
            .OrderBy(r => r.Id)
            .Select(r => checked((int?)H24lClientCastFunctions.MaybeMoney(r.Amount)))
            .ToList();

        List<int?> actual = db.Table<H24lClientCastRow>()
            .OrderBy(r => r.Id)
            .Select(r => checked((int?)H24lClientCastFunctions.MaybeMoney(r.Amount)))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProjectsANullClientValueThroughALiftedConversionOperator()
    {
        using TestDatabase db = Seed();
        List<H24lClientCastRow> local = Rows();

        List<int?> expected = local
            .OrderBy(r => r.Id)
            .Select(r => (int?)H24lClientCastFunctions.MaybeMoney(r.Amount))
            .ToList();

        List<int?> actual = db.Table<H24lClientCastRow>()
            .OrderBy(r => r.Id)
            .Select(r => (int?)H24lClientCastFunctions.MaybeMoney(r.Amount))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H24lClientCastRow> Rows()
    {
        return
        [
            new H24lClientCastRow { Id = 1, Amount = 70000 },
            new H24lClientCastRow { Id = 2, Amount = 65 }
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<H24lClientCastRow>().Schema.CreateTable();
        db.Table<H24lClientCastRow>().AddRange(Rows());
        return db;
    }
}
