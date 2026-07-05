using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("DowClientEval")]
public class DowClientEvalRow
{
    [Key]
    public int Id { get; set; }

    public DateTime When { get; set; }
}

public class DayOfWeekClientEvalEqualsTests
{
    private static DateTime Shift(DateTime d)
    {
        return d.AddDays(1);
    }

    [Fact]
    public void ComputedDayOfWeekEqualsThroughClientHelper()
    {
        using TestDatabase db = new();
        db.Table<DowClientEvalRow>().Schema.CreateTable();
        db.Table<DowClientEvalRow>().Add(new DowClientEvalRow { Id = 1, When = new DateTime(2024, 1, 7) });

        List<DowClientEvalRow> mem = [new() { Id = 1, When = new DateTime(2024, 1, 7) }];
        List<bool> expected = mem.Select(r => Shift(r.When).DayOfWeek.Equals(DayOfWeek.Monday)).ToList();
        List<bool> actual = db.Table<DowClientEvalRow>().Select(r => Shift(r.When).DayOfWeek.Equals(DayOfWeek.Monday)).ToList();

        Assert.Equal(expected, actual);
    }
}
