using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25lSpanUnitRows")]
public class H25lSpanUnitRow
{
    [Key]
    public int Id { get; set; }

    public TimeSpan Span { get; set; }

    public string Name { get; set; } = "";
}

public class TimeSpanFromUnitComponentArgumentTests
{
    [Fact]
    public void BuildingASpanFromTheHoursComponentKeepsTheComponentValue()
    {
        using TestDatabase db = Setup(nameof(BuildingASpanFromTheHoursComponentKeepsTheComponentValue));

        List<long> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => TimeSpan.FromHours(r.Span.Hours).Ticks)
            .ToList();

        List<long> actual = db.Table<H25lSpanUnitRow>()
            .OrderBy(r => r.Id)
            .Select(r => TimeSpan.FromHours(r.Span.Hours).Ticks)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BuildingASpanFromTheMillisecondsComponentKeepsTheComponentValue()
    {
        using TestDatabase db = Setup(nameof(BuildingASpanFromTheMillisecondsComponentKeepsTheComponentValue));

        List<long> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => TimeSpan.FromMilliseconds(r.Span.Milliseconds).Ticks)
            .ToList();

        List<long> actual = db.Table<H25lSpanUnitRow>()
            .OrderBy(r => r.Id)
            .Select(r => TimeSpan.FromMilliseconds(r.Span.Milliseconds).Ticks)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BuildingASpanFromTheMicrosecondsComponentKeepsTheComponentValue()
    {
        using TestDatabase db = Setup(nameof(BuildingASpanFromTheMicrosecondsComponentKeepsTheComponentValue));

        List<long> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => TimeSpan.FromMicroseconds(r.Span.Microseconds).Ticks)
            .ToList();

        List<long> actual = db.Table<H25lSpanUnitRow>()
            .OrderBy(r => r.Id)
            .Select(r => TimeSpan.FromMicroseconds(r.Span.Microseconds).Ticks)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BuildingASpanFromACharacterPositionKeepsThePositionValue()
    {
        using TestDatabase db = Setup(nameof(BuildingASpanFromACharacterPositionKeepsThePositionValue));

        List<long> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => TimeSpan.FromDays(r.Name.IndexOf('c')).Ticks)
            .ToList();

        List<long> actual = db.Table<H25lSpanUnitRow>()
            .OrderBy(r => r.Id)
            .Select(r => TimeSpan.FromDays(r.Name.IndexOf('c')).Ticks)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H25lSpanUnitRow> Rows()
    {
        return
        [
            new H25lSpanUnitRow { Id = 1, Span = new TimeSpan(30, 15, 40) + TimeSpan.FromTicks(2345678), Name = "abcde" },
            new H25lSpanUnitRow { Id = 2, Span = TimeSpan.FromTicks(9999999), Name = "abcabc" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H25lSpanUnitRow>().Schema.CreateTable();
        db.Table<H25lSpanUnitRow>().AddRange(Rows());
        return db;
    }
}
