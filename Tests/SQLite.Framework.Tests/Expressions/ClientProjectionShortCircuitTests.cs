using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25mShortCircuitRows")]
public class H25mShortCircuitRow
{
    [Key]
    public int Id { get; set; }

    public int Divisor { get; set; }

    public int? Cached { get; set; }
}

public static class H25mShortCircuitMath
{
    public static int Reciprocal(int value)
    {
        return 100 / value;
    }
}

public class ClientProjectionShortCircuitTests
{
    [Fact]
    public void AndAlsoLeavesTheRightSideAloneWhenTheLeftSideIsFalse()
    {
        using TestDatabase db = Setup(nameof(AndAlsoLeavesTheRightSideAloneWhenTheLeftSideIsFalse));

        List<bool> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => r.Divisor != 0 && H25mShortCircuitMath.Reciprocal(r.Divisor) > 5)
            .ToList();

        List<bool> actual = db.Table<H25mShortCircuitRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Divisor != 0 && H25mShortCircuitMath.Reciprocal(r.Divisor) > 5)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrElseLeavesTheRightSideAloneWhenTheLeftSideIsTrue()
    {
        using TestDatabase db = Setup(nameof(OrElseLeavesTheRightSideAloneWhenTheLeftSideIsTrue));

        List<bool> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => r.Divisor == 0 || H25mShortCircuitMath.Reciprocal(r.Divisor) > 5)
            .ToList();

        List<bool> actual = db.Table<H25mShortCircuitRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Divisor == 0 || H25mShortCircuitMath.Reciprocal(r.Divisor) > 5)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CoalesceLeavesTheRightSideAloneWhenTheLeftSideHasAValue()
    {
        using TestDatabase db = Setup(nameof(CoalesceLeavesTheRightSideAloneWhenTheLeftSideHasAValue));

        List<int> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => r.Cached ?? H25mShortCircuitMath.Reciprocal(r.Divisor))
            .ToList();

        List<int> actual = db.Table<H25mShortCircuitRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Cached ?? H25mShortCircuitMath.Reciprocal(r.Divisor))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H25mShortCircuitRow> Rows()
    {
        return
        [
            new H25mShortCircuitRow { Id = 1, Divisor = 0, Cached = 7 },
            new H25mShortCircuitRow { Id = 2, Divisor = 4, Cached = null },
            new H25mShortCircuitRow { Id = 3, Divisor = 50, Cached = 9 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(o =>
        {
            o.SelectMaterializers.Clear();
            o.ReflectionFallbackDisabled = false;
        }, methodName);
        db.Table<H25mShortCircuitRow>().Schema.CreateTable();
        db.Table<H25mShortCircuitRow>().AddRange(Rows());
        return db;
    }
}
