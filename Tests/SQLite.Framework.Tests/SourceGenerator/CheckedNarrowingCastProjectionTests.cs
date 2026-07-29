using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24mCheckedCastRows")]
public class H24mCheckedCastRow
{
    [Key]
    public int Id { get; set; }

    public int Amount { get; set; }
}

public static class H24mCheckedCastFunctions
{
    public static int Pass(int value)
    {
        return value;
    }

    public static string Describe(byte value)
    {
        return "b=" + value;
    }
}

public class CheckedNarrowingCastProjectionTests
{
    [Fact]
    public void CheckedCastAroundAClientCallMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<byte> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => checked((byte)H24mCheckedCastFunctions.Pass(r.Amount)))
            .ToList();

        List<byte> actual = db.Table<H24mCheckedCastRow>()
            .OrderBy(r => r.Id)
            .Select(r => checked((byte)H24mCheckedCastFunctions.Pass(r.Amount)))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CheckedCastInsideAClientCallArgumentMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => H24mCheckedCastFunctions.Describe(checked((byte)r.Amount)))
            .ToList();

        List<string> actual = db.Table<H24mCheckedCastRow>()
            .OrderBy(r => r.Id)
            .Select(r => H24mCheckedCastFunctions.Describe(checked((byte)r.Amount)))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H24mCheckedCastRow> Rows()
    {
        return
        [
            new H24mCheckedCastRow { Id = 1, Amount = 7 },
            new H24mCheckedCastRow { Id = 2, Amount = 44 }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H24mCheckedCastRow>().Schema.CreateTable();
        db.Table<H24mCheckedCastRow>().AddRange(Rows());
        return db;
    }
}
