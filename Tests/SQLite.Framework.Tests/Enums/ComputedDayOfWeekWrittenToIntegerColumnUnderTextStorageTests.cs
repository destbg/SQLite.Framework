using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26oDowNumberWrites")]
public class H26oDowNumberWrite
{
    [Key]
    public int Id { get; set; }

    public DateTime When { get; set; }

    public int DowNumber { get; set; }
}

public class ComputedDayOfWeekWrittenToIntegerColumnUnderTextStorageTests
{
    [Fact]
    public void ExecuteUpdateWritesTheDayNumberIntoAnIntegerColumnWhenEnumsAreStoredAsText()
    {
        using TestDatabase db = Setup(nameof(ExecuteUpdateWritesTheDayNumberIntoAnIntegerColumnWhenEnumsAreStoredAsText));

        db.Table<H26oDowNumberWrite>().ExecuteUpdate(s => s.Set(r => r.DowNumber, r => (int)r.When.DayOfWeek));

        List<int> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => (int)r.When.DayOfWeek)
            .ToList();

        List<int> actual = db.Table<H26oDowNumberWrite>()
            .OrderBy(r => r.Id)
            .Select(r => r.DowNumber)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H26oDowNumberWrite> Rows()
    {
        return
        [
            new H26oDowNumberWrite { Id = 1, When = new DateTime(2024, 1, 1, 9, 0, 0) },
            new H26oDowNumberWrite { Id = 2, When = new DateTime(2024, 1, 2, 9, 0, 0) },
            new H26oDowNumberWrite { Id = 3, When = new DateTime(2024, 1, 6, 9, 0, 0) },
            new H26oDowNumberWrite { Id = 4, When = new DateTime(2024, 1, 7, 9, 0, 0) }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text), methodName);
        db.Table<H26oDowNumberWrite>().Schema.CreateTable();
        db.Table<H26oDowNumberWrite>().AddRange(Rows());
        return db;
    }
}
