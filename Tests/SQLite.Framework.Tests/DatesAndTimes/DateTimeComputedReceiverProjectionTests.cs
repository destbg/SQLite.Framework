using System.ComponentModel.DataAnnotations;
using System.Globalization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ComputedReceiverDateRow
{
    [Key]
    public int Id { get; set; }

    public DateTime When { get; set; }
}

public class DateTimeComputedReceiverProjectionTests
{
    private static List<ComputedReceiverDateRow> Rows() =>
    [
        new() { Id = 1, When = new DateTime(2024, 5, 10, 8, 0, 0) },
        new() { Id = 2, When = new DateTime(2023, 1, 2, 3, 4, 5) },
    ];

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<ComputedReceiverDateRow>().Schema.CreateTable();
        db.Table<ComputedReceiverDateRow>().AddRange(Rows());
        return db;
    }

    private static int HoursFor(int id)
    {
        return id * 2;
    }

    [Fact]
    public void ToStringOfAddYearsInSelectProjects()
    {
        using TestDatabase db = Seed();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => r.When.AddYears(1).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))
            .ToList();
        Assert.Equal(["2025-05-10 08:00:00", "2024-01-02 03:04:05"], expected);

        List<string> actual = db.Table<ComputedReceiverDateRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.When.AddYears(1).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AddHoursStaticHelperAfterAddYearsInSelectProjects()
    {
        using TestDatabase db = Seed();

        List<DateTime> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => r.When.AddYears(1).AddHours(HoursFor(r.Id)))
            .ToList();
        Assert.Equal([new DateTime(2025, 5, 10, 10, 0, 0), new DateTime(2024, 1, 2, 7, 4, 5)], expected);

        List<DateTime> actual = db.Table<ComputedReceiverDateRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.When.AddYears(1).AddHours(HoursFor(r.Id)))
            .ToList();
        Assert.Equal(expected, actual);
    }
}
