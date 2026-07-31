using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26rWindowDayRows")]
public class H26rWindowDayRow
{
    [Key]
    public int Id { get; set; }

    public DateTime When { get; set; }
}

public class H26rWindowDayProjection
{
    public int Id { get; set; }

    public DayOfWeek Day { get; set; }
}

public class TypedProjectionWindowDayOfWeekFilterTests
{
    [Fact]
    public void AWindowDayOfWeekReadIntoATypedPropertyFiltersOnTheSameDay()
    {
        using TestDatabase db = Setup();

        List<H26rWindowDayRow> ordered = Rows().OrderBy(r => r.Id).ToList();
        DayOfWeek firstDay = ordered[0].When.DayOfWeek;
        int expected = ordered.Count(_ => firstDay == DayOfWeek.Monday);

        Assert.Equal(4, expected);

        int actual = db.Table<H26rWindowDayRow>()
            .Select(r => new H26rWindowDayProjection
            {
                Id = r.Id,
                Day = SQLiteWindowFunctions.FirstValue(r.When.DayOfWeek)
                    .Over()
                    .OrderBy(r.Id)
            })
            .Where(x => x.Day == DayOfWeek.Monday)
            .Count();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AWindowDayOfWeekReadThroughAsValueFiltersOnTheSameDay()
    {
        using TestDatabase db = Setup();

        List<H26rWindowDayRow> ordered = Rows().OrderBy(r => r.Id).ToList();
        DayOfWeek firstDay = ordered[0].When.DayOfWeek;
        int expected = ordered.Count(_ => firstDay == DayOfWeek.Monday);

        int actual = db.Table<H26rWindowDayRow>()
            .Select(r => new
            {
                r.Id,
                Day = SQLiteWindowFunctions.FirstValue(r.When.DayOfWeek)
                    .Over()
                    .OrderBy(r.Id)
                    .AsValue()
            })
            .Where(x => x.Day == DayOfWeek.Monday)
            .Count();

        Assert.Equal(expected, actual);
    }

    private static List<H26rWindowDayRow> Rows()
    {
        return
        [
            new H26rWindowDayRow { Id = 1, When = new DateTime(2024, 1, 1) },
            new H26rWindowDayRow { Id = 2, When = new DateTime(2024, 1, 2) },
            new H26rWindowDayRow { Id = 3, When = new DateTime(2024, 1, 5) },
            new H26rWindowDayRow { Id = 4, When = new DateTime(2024, 1, 7) }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Table<H26rWindowDayRow>().Schema.CreateTable();
        db.Table<H26rWindowDayRow>().AddRange(Rows());
        return db;
    }
}
