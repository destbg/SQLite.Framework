using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class CharPairRow
{
    [Key]
    public int Id { get; set; }

    public char A { get; set; }

    public char B { get; set; }
}

internal sealed class TimeSpanTextArithmeticRow
{
    [Key]
    public int Id { get; set; }

    public int Value { get; set; }
}

internal sealed class NullableDateArithmeticRow
{
    [Key]
    public int Id { get; set; }

    public DateTime? When { get; set; }
}

public class SqlVisitorBinaryBranchTests
{
    [Fact]
    public void TwoCharColumnsCompareForEquality()
    {
        using TestDatabase db = new();
        db.Table<CharPairRow>().Schema.CreateTable();
        db.Table<CharPairRow>().Add(new CharPairRow { Id = 1, A = 'x', B = 'x' });
        db.Table<CharPairRow>().Add(new CharPairRow { Id = 2, A = 'y', B = 'z' });

        List<int> expected = db.Table<CharPairRow>().AsEnumerable()
            .Where(r => r.A == r.B)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([1], expected);

        List<int> actual = db.Table<CharPairRow>()
            .Where(r => r.A == r.B)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IntegerArithmeticUnderTextTimeSpanStorage()
    {
        using TestDatabase db = new(b => b.UseTimeSpanStorage(TimeSpanStorageMode.Text));
        db.Table<TimeSpanTextArithmeticRow>().Schema.CreateTable();
        db.Table<TimeSpanTextArithmeticRow>().Add(new TimeSpanTextArithmeticRow { Id = 1, Value = 10 });
        db.Table<TimeSpanTextArithmeticRow>().Add(new TimeSpanTextArithmeticRow { Id = 2, Value = 20 });

        List<int> expected = db.Table<TimeSpanTextArithmeticRow>().AsEnumerable()
            .Where(r => r.Value + 1 > 15)
            .Select(r => r.Value)
            .ToList();

        Assert.Equal([20], expected);

        List<int> actual = db.Table<TimeSpanTextArithmeticRow>()
            .Where(r => r.Value + 1 > 15)
            .Select(r => r.Value)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableDateTimePlusTimeSpanUnderTextTimeSpanStorage()
    {
        using TestDatabase db = new(b => b.UseTimeSpanStorage(TimeSpanStorageMode.Text));
        db.Table<NullableDateArithmeticRow>().Schema.CreateTable();
        db.Table<NullableDateArithmeticRow>().Add(new NullableDateArithmeticRow { Id = 1, When = new DateTime(2020, 1, 1) });
        db.Table<NullableDateArithmeticRow>().Add(new NullableDateArithmeticRow { Id = 2, When = null });

        List<DateTime?> expected = db.Table<NullableDateArithmeticRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => r.When + TimeSpan.FromDays(1))
            .ToList();

        Assert.Equal([new DateTime(2020, 1, 2), null], expected);

        List<DateTime?> actual = db.Table<NullableDateArithmeticRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.When + TimeSpan.FromDays(1))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
