using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25qDowOwners")]
public class H25qDowOwner
{
    [Key]
    public int Id { get; set; }
}

[Table("H25qDowEvents")]
public class H25qDowEvent
{
    [Key]
    public int Id { get; set; }

    public int OwnerId { get; set; }

    public DateTime When { get; set; }
}

public class CorrelatedSubqueryComputedDayOfWeekComparisonTests
{
    [Fact]
    public void ComparingACorrelatedSubqueryDayOfWeekAgainstAConstantKeepsTheLinqRows()
    {
        using TestDatabase db = Setup(nameof(ComparingACorrelatedSubqueryDayOfWeekAgainstAConstantKeepsTheLinqRows));

        List<int> expected = Owners()
            .Where(o => Events().Where(e => e.OwnerId == o.Id).Select(e => e.When.DayOfWeek).First() == DayOfWeek.Monday)
            .Select(o => o.Id)
            .OrderBy(v => v)
            .ToList();

        Assert.Equal([1, 3], expected);

        List<int> actual = db.Table<H25qDowOwner>()
            .Where(o => db.Table<H25qDowEvent>()
                .Where(e => e.OwnerId == o.Id)
                .Select(e => e.When.DayOfWeek)
                .First() == DayOfWeek.Monday)
            .Select(o => o.Id)
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ComparingACorrelatedSubqueryDayOfWeekAgainstAConstantInAProjectionKeepsTheLinqValues()
    {
        using TestDatabase db = Setup(nameof(ComparingACorrelatedSubqueryDayOfWeekAgainstAConstantInAProjectionKeepsTheLinqValues));

        List<bool> expected = Owners()
            .OrderBy(o => o.Id)
            .Select(o => Events().Where(e => e.OwnerId == o.Id).Select(e => e.When.DayOfWeek).First() == DayOfWeek.Monday)
            .ToList();

        Assert.Equal([true, false, true], expected);

        List<bool> actual = db.Table<H25qDowOwner>()
            .OrderBy(o => o.Id)
            .Select(o => db.Table<H25qDowEvent>()
                .Where(e => e.OwnerId == o.Id)
                .Select(e => e.When.DayOfWeek)
                .First() == DayOfWeek.Monday)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CastingACorrelatedSubqueryDayOfWeekToAnIntegerKeepsTheLinqNumbers()
    {
        using TestDatabase db = Setup(nameof(CastingACorrelatedSubqueryDayOfWeekToAnIntegerKeepsTheLinqNumbers));

        List<int> expected = Owners()
            .OrderBy(o => o.Id)
            .Select(o => (int)Events().Where(e => e.OwnerId == o.Id).Select(e => e.When.DayOfWeek).First())
            .ToList();

        Assert.Equal([1, 2, 1], expected);

        List<int> actual = db.Table<H25qDowOwner>()
            .OrderBy(o => o.Id)
            .Select(o => (int)db.Table<H25qDowEvent>()
                .Where(e => e.OwnerId == o.Id)
                .Select(e => e.When.DayOfWeek)
                .First())
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H25qDowOwner> Owners()
    {
        return
        [
            new H25qDowOwner { Id = 1 },
            new H25qDowOwner { Id = 2 },
            new H25qDowOwner { Id = 3 }
        ];
    }

    private static List<H25qDowEvent> Events()
    {
        return
        [
            new H25qDowEvent { Id = 11, OwnerId = 1, When = new DateTime(2024, 1, 1, 9, 0, 0) },
            new H25qDowEvent { Id = 12, OwnerId = 2, When = new DateTime(2024, 1, 2, 9, 0, 0) },
            new H25qDowEvent { Id = 13, OwnerId = 3, When = new DateTime(2024, 1, 8, 9, 0, 0) }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text), methodName);
        db.Table<H25qDowOwner>().Schema.CreateTable();
        db.Table<H25qDowEvent>().Schema.CreateTable();
        db.Table<H25qDowOwner>().AddRange(Owners());
        db.Table<H25qDowEvent>().AddRange(Events());
        return db;
    }
}
