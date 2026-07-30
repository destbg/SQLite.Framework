using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26aDowIntWrites")]
public class H26aDowIntWrite
{
    [Key]
    public int Id { get; set; }

    public DateTime When { get; set; }

    public DayOfWeek Dow { get; set; }

    public int DowNumber { get; set; }
}

public class ComputedDayOfWeekWrittenToIntegerColumnTests
{
    [Fact]
    public void ExecuteUpdateSettingAnEnumColumnFromAComputedDayOfWeekStoresTheNumberForm()
    {
        using TestDatabase db = Setup(nameof(ExecuteUpdateSettingAnEnumColumnFromAComputedDayOfWeekStoresTheNumberForm));

        db.Table<H26aDowIntWrite>().ExecuteUpdate(s => s.Set(r => r.Dow, r => r.When.DayOfWeek));

        List<int> expected = WriteRows()
            .Select(r => new H26aDowIntWrite { Id = r.Id, When = r.When, Dow = r.When.DayOfWeek })
            .Where(r => r.Dow == DayOfWeek.Monday)
            .Select(r => r.Id)
            .OrderBy(v => v)
            .ToList();

        Assert.Equal([1, 3], expected);

        List<int> actual = db.Table<H26aDowIntWrite>()
            .Where(r => r.Dow == DayOfWeek.Monday)
            .Select(r => r.Id)
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ExecuteUpdateSettingAnIntColumnFromAComputedDayOfWeekStoresTheNumber()
    {
        using TestDatabase db = Setup(nameof(ExecuteUpdateSettingAnIntColumnFromAComputedDayOfWeekStoresTheNumber));

        db.Table<H26aDowIntWrite>().ExecuteUpdate(s => s.Set(r => r.DowNumber, r => (int)r.When.DayOfWeek));

        List<int> expected = WriteRows()
            .OrderBy(r => r.Id)
            .Select(r => (int)r.When.DayOfWeek)
            .ToList();

        Assert.Equal([1, 2, 1], expected);

        List<int> actual = db.Table<H26aDowIntWrite>()
            .OrderBy(r => r.Id)
            .Select(r => r.DowNumber)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H26aDowIntWrite> WriteRows()
    {
        return
        [
            new H26aDowIntWrite { Id = 1, When = new DateTime(2024, 1, 1, 9, 0, 0) },
            new H26aDowIntWrite { Id = 2, When = new DateTime(2024, 1, 2, 9, 0, 0) },
            new H26aDowIntWrite { Id = 3, When = new DateTime(2024, 1, 8, 9, 0, 0) }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H26aDowIntWrite>().Schema.CreateTable();
        db.Table<H26aDowIntWrite>().AddRange(WriteRows());
        return db;
    }
}
