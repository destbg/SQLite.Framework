using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H20DowScalarCte")]
public class H20DowScalarCteRow
{
    [Key]
    public int Id { get; set; }

    public DateTime When { get; set; }

    public DayOfWeek Day { get; set; }
}

public class ScalarCteDayOfWeekComparisonTests
{
    private static List<H20DowScalarCteRow> Rows()
    {
        return
        [
            new H20DowScalarCteRow { Id = 1, When = new DateTime(2024, 1, 1), Day = DayOfWeek.Wednesday },
            new H20DowScalarCteRow { Id = 2, When = new DateTime(2024, 1, 2), Day = DayOfWeek.Tuesday },
            new H20DowScalarCteRow { Id = 3, When = new DateTime(2024, 1, 4), Day = DayOfWeek.Sunday },
            new H20DowScalarCteRow { Id = 4, When = new DateTime(2024, 1, 5), Day = DayOfWeek.Thursday },
            new H20DowScalarCteRow { Id = 5, When = new DateTime(2024, 1, 6), Day = DayOfWeek.Monday },
            new H20DowScalarCteRow { Id = 6, When = new DateTime(2024, 1, 7), Day = DayOfWeek.Sunday },
        ];
    }

    private static TestDatabase Setup(EnumStorageMode mode)
    {
        TestDatabase db = new(b => b.UseEnumStorage(mode));
        db.Table<H20DowScalarCteRow>().Schema.CreateTable();
        db.Table<H20DowScalarCteRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void ScalarCteComputedDayOfWeekWhereConstantTextStorageMatchesLinq()
    {
        using TestDatabase db = Setup(EnumStorageMode.Text);
        SQLiteCte<DayOfWeek> cte = db.With<DayOfWeek>(() =>
            db.Table<H20DowScalarCteRow>().Select(r => r.When.DayOfWeek));

        List<DayOfWeek> expected = Rows()
            .Select(r => r.When.DayOfWeek)
            .Where(d => d == DayOfWeek.Thursday || d == DayOfWeek.Monday)
            .OrderBy(d => d).ToList();

        List<DayOfWeek> actual = cte
            .Where(d => d == DayOfWeek.Thursday || d == DayOfWeek.Monday)
            .OrderBy(d => d).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ScalarCteComputedDayOfWeekJoinStoredColumnTextStorageMatchesLinq()
    {
        using TestDatabase db = Setup(EnumStorageMode.Text);
        SQLiteCte<DayOfWeek> cte = db.With<DayOfWeek>(() =>
            db.Table<H20DowScalarCteRow>().Select(r => r.When.DayOfWeek));

        List<int> expected = Rows()
            .Select(r => r.When.DayOfWeek)
            .Join(Rows(), d => d, r => r.Day, (d, r) => r.Id)
            .OrderBy(i => i).ToList();

        List<int> actual = cte
            .Join(db.Table<H20DowScalarCteRow>(), d => d, r => r.Day, (d, r) => r.Id)
            .OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ScalarCteComputedDayOfWeekContainsStoredColumnTextStorageMatchesLinq()
    {
        using TestDatabase db = Setup(EnumStorageMode.Text);
        SQLiteCte<DayOfWeek> cte = db.With<DayOfWeek>(() =>
            db.Table<H20DowScalarCteRow>().Select(r => r.When.DayOfWeek));

        List<int> expected = Rows()
            .Where(r => Rows().Select(x => x.When.DayOfWeek).Contains(r.Day))
            .Select(r => r.Id).OrderBy(i => i).ToList();

        List<int> actual = db.Table<H20DowScalarCteRow>()
            .Where(r => cte.Contains(r.Day))
            .Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ScalarCteComputedDayOfWeekWhereConstantIntegerStorageMatchesLinq()
    {
        using TestDatabase db = Setup(EnumStorageMode.Integer);
        SQLiteCte<DayOfWeek> cte = db.With<DayOfWeek>(() =>
            db.Table<H20DowScalarCteRow>().Select(r => r.When.DayOfWeek));

        List<DayOfWeek> expected = Rows()
            .Select(r => r.When.DayOfWeek)
            .Where(d => d == DayOfWeek.Thursday || d == DayOfWeek.Monday)
            .OrderBy(d => d).ToList();

        List<DayOfWeek> actual = cte
            .Where(d => d == DayOfWeek.Thursday || d == DayOfWeek.Monday)
            .OrderBy(d => d).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ScalarCteStoredDayOfWeekContainsStoredColumnTextStorageMatchesLinq()
    {
        using TestDatabase db = Setup(EnumStorageMode.Text);
        SQLiteCte<DayOfWeek> cte = db.With<DayOfWeek>(() =>
            db.Table<H20DowScalarCteRow>().Where(r => r.Id > 3).Select(r => r.Day));

        List<int> expected = Rows()
            .Where(r => Rows().Where(x => x.Id > 3).Select(x => x.Day).Contains(r.Day))
            .Select(r => r.Id).OrderBy(i => i).ToList();
        Assert.Equal([3, 4, 5, 6], expected);

        List<int> actual = db.Table<H20DowScalarCteRow>()
            .Where(r => cte.Contains(r.Day))
            .Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }
}
