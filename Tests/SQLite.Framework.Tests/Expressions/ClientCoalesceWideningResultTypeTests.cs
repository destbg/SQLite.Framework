using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26mCoalesceWideningRows")]
public class H26mCoalesceWideningRow
{
    [Key]
    public int Id { get; set; }

    public int Divisor { get; set; }

    public int? Cached { get; set; }
}

public static class H26mWideningMath
{
    public static long Scale(int value)
    {
        return value * 1000L;
    }

    public static double Half(int value)
    {
        return value / 2.0;
    }

    public static decimal Tenth(int value)
    {
        return value / 10m;
    }
}

public class ClientCoalesceWideningResultTypeTests
{
    [Fact]
    public void ACoalesceOverAnIntegerColumnAndALongClientCallProducesLongValues()
    {
        using TestDatabase db = Setup(nameof(ACoalesceOverAnIntegerColumnAndALongClientCallProducesLongValues));

        List<long> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => r.Cached ?? H26mWideningMath.Scale(r.Divisor))
            .ToList();

        List<long> actual = db.Table<H26mCoalesceWideningRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Cached ?? H26mWideningMath.Scale(r.Divisor))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ACoalesceOverAnIntegerColumnAndADoubleClientCallProducesDoubleValues()
    {
        using TestDatabase db = Setup(nameof(ACoalesceOverAnIntegerColumnAndADoubleClientCallProducesDoubleValues));

        List<double> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => r.Cached ?? H26mWideningMath.Half(r.Divisor))
            .ToList();

        List<double> actual = db.Table<H26mCoalesceWideningRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Cached ?? H26mWideningMath.Half(r.Divisor))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ACoalesceOverAnIntegerColumnAndADecimalClientCallProducesDecimalValues()
    {
        using TestDatabase db = Setup(nameof(ACoalesceOverAnIntegerColumnAndADecimalClientCallProducesDecimalValues));

        List<decimal> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => r.Cached ?? H26mWideningMath.Tenth(r.Divisor))
            .ToList();

        List<decimal> actual = db.Table<H26mCoalesceWideningRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Cached ?? H26mWideningMath.Tenth(r.Divisor))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H26mCoalesceWideningRow> Rows()
    {
        return
        [
            new H26mCoalesceWideningRow { Id = 1, Divisor = 3, Cached = 7 },
            new H26mCoalesceWideningRow { Id = 2, Divisor = 4, Cached = null },
            new H26mCoalesceWideningRow { Id = 3, Divisor = 5, Cached = 9 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(o =>
        {
            o.SelectMaterializers.Clear();
            o.ReflectionFallbackDisabled = false;
        }, methodName);
        db.Table<H26mCoalesceWideningRow>().Schema.CreateTable();
        db.Table<H26mCoalesceWideningRow>().AddRange(Rows());
        return db;
    }
}
