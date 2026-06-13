using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class CapturedCastMeasurementRow
{
    [Key]
    public int Id { get; set; }

    public int Value { get; set; }

    public long LongValue { get; set; }
}

public class CapturedFloatingPointCastConstantTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Table<CapturedCastMeasurementRow>().Schema.CreateTable();
        db.Table<CapturedCastMeasurementRow>().Add(new CapturedCastMeasurementRow { Id = 1, Value = 2, LongValue = 2 });
        db.Table<CapturedCastMeasurementRow>().Add(new CapturedCastMeasurementRow { Id = 2, Value = 3, LongValue = 3 });
        return db;
    }

    [Fact]
    public void CapturedDoubleCastToIntTruncatesTowardZero()
    {
        using TestDatabase db = SetupDatabase();
        double local = 2.7;

        List<int> expected = db.Table<CapturedCastMeasurementRow>().AsEnumerable()
            .Where(r => r.Value == (int)local)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([1], expected);

        List<int> actual = db.Table<CapturedCastMeasurementRow>()
            .Where(r => r.Value == (int)local)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedNegativeDoubleCastToIntTruncatesTowardZero()
    {
        using TestDatabase db = SetupDatabase();
        double local = -2.7;

        List<int> expected = db.Table<CapturedCastMeasurementRow>().AsEnumerable()
            .Where(r => r.Value == -(int)local)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([1], expected);

        List<int> actual = db.Table<CapturedCastMeasurementRow>()
            .Where(r => r.Value == -(int)local)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedFloatCastToIntTruncatesTowardZero()
    {
        using TestDatabase db = SetupDatabase();
        float local = 2.7f;

        List<int> expected = db.Table<CapturedCastMeasurementRow>().AsEnumerable()
            .Where(r => r.Value == (int)local)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([1], expected);

        List<int> actual = db.Table<CapturedCastMeasurementRow>()
            .Where(r => r.Value == (int)local)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedDecimalCastToIntTruncatesTowardZero()
    {
        using TestDatabase db = SetupDatabase();
        decimal local = 2.7m;

        List<int> expected = db.Table<CapturedCastMeasurementRow>().AsEnumerable()
            .Where(r => r.Value == (int)local)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([1], expected);

        List<int> actual = db.Table<CapturedCastMeasurementRow>()
            .Where(r => r.Value == (int)local)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedDoubleCastToLongTruncatesTowardZero()
    {
        using TestDatabase db = SetupDatabase();
        double local = 2.7;

        List<int> expected = db.Table<CapturedCastMeasurementRow>().AsEnumerable()
            .Where(r => r.LongValue == (long)local)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([1], expected);

        List<int> actual = db.Table<CapturedCastMeasurementRow>()
            .Where(r => r.LongValue == (long)local)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
