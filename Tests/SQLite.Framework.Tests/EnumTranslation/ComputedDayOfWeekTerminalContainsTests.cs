using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26uShiftRows")]
public class H26uShiftRow
{
    [Key]
    public int Id { get; set; }

    public DateTime When { get; set; }
}

public class ComputedDayOfWeekTerminalContainsTests
{
    [Fact]
    public void ContainsFindsAComputedDayOfWeekThatMatchesSeveralRows()
    {
        using TestDatabase db = Setup(nameof(ContainsFindsAComputedDayOfWeekThatMatchesSeveralRows));

        bool expected = Rows().Select(r => r.When.DayOfWeek).Contains(DayOfWeek.Monday);
        bool actual = db.Table<H26uShiftRow>().Select(r => r.When.DayOfWeek).Contains(DayOfWeek.Monday);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ContainsFindsAComputedDayOfWeekThatMatchesOneRow()
    {
        using TestDatabase db = Setup(nameof(ContainsFindsAComputedDayOfWeekThatMatchesOneRow));

        bool expected = Rows().Select(r => r.When.DayOfWeek).Contains(DayOfWeek.Tuesday);
        bool actual = db.Table<H26uShiftRow>().Select(r => r.When.DayOfWeek).Contains(DayOfWeek.Tuesday);

        Assert.Equal(expected, actual);
    }

    private static List<H26uShiftRow> Rows()
    {
        return
        [
            new H26uShiftRow { Id = 1, When = new DateTime(2024, 1, 1) },
            new H26uShiftRow { Id = 2, When = new DateTime(2024, 1, 2) },
            new H26uShiftRow { Id = 3, When = new DateTime(2024, 1, 8) }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.EnumStorage = EnumStorageMode.Text, methodName);
        db.Table<H26uShiftRow>().Schema.CreateTable();
        db.Table<H26uShiftRow>().AddRange(Rows());
        return db;
    }
}
